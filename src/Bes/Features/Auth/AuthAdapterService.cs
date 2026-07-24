using System.Text;
using Bardie.Auth.V1;
using Bardie.Module.Auth;
using Bardie.Module.Channel.Manifest;
using Google.Protobuf;
using Grpc.Core;

namespace Bes.Features.Auth;

/// <summary>AuthAdapter gRPC façade — commands live in password + JWT services.</summary>
public sealed class AuthAdapterService : AuthAdapterModuleBase
{
    private static readonly string[] DefaultRoles = ["user"];
    private static readonly string[] AdminRoles = ["admin"];

    private readonly BesPasswordService _passwords;
    private readonly AuthModuleJwtService _tokens;
    private readonly ILogger<AuthAdapterService> _logger;

    public AuthAdapterService(
        ModuleManifest manifest,
        BesPasswordService passwords,
        AuthModuleJwtService tokens,
        ILogger<AuthAdapterService> logger)
        : base(manifest)
    {
        _passwords = passwords;
        _tokens = tokens;
        _logger = logger;
    }

    public override Task<GetProvidersResponse> GetProviders(GetProvidersRequest request, ServerCallContext context)
    {
        var response = new GetProvidersResponse();
        response.Providers.Add(new ProviderDescriptor
        {
            Id = Manifest.Slug,
            DisplayName = string.IsNullOrWhiteSpace(Manifest.DisplayName) ? "Bes" : Manifest.DisplayName,
            FormSchema = ModuleManifestAuthBag.TryBuildFormSchema(Manifest)
                ?? throw new InvalidOperationException(
                    "Bes module.manifest.json must declare auth.formFields for GetProviders."),
        });
        return Task.FromResult(response);
    }

    public override Task<AuthenticateResponse> Authenticate(AuthenticateRequest request, ServerCallContext context)
    {
        if (!MatchesProviderId(request.ProviderId))
        {
            return Task.FromResult(Denied());
        }

        request.Payload.TryGetValue("username", out var username);
        request.Payload.TryGetValue("password", out var password);
        request.Payload.TryGetValue("new_password", out var newPassword);
        username = username?.Trim();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return Task.FromResult(Denied());
        }

        var binding = BesPasswordService.TryReadBinding(request.ExistingBindingPayload.Span);
        if (binding is null)
        {
            // No binding yet — login requires a prior SeedAdmin (or future selfRegister).
            _logger.LogInformation("Authenticate rejected for {User}: no binding payload.", username);
            return Task.FromResult(Denied());
        }

        if (!_passwords.Verify(binding.PasswordHash, password))
        {
            _logger.LogInformation("Authenticate rejected for {User}: bad password.", username);
            return Task.FromResult(Denied());
        }

        // AUTH-ROLE-001: roles from binding; SeedAdmin alone creates admin. Missing roles → user.
        var roles = binding.Roles.Count > 0 ? binding.Roles.ToArray() : DefaultRoles;
        var mustRotate = binding.MustRotate;
        var passwordHash = binding.PasswordHash;

        // AUTH-ROT-001: password change clears must_rotate (Authenticate bag: new_password).
        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            if (newPassword.Length < 8)
            {
                _logger.LogInformation("Authenticate rejected for {User}: new_password too short.", username);
                return Task.FromResult(Denied());
            }

            passwordHash = _passwords.HashPassword(newPassword);
            mustRotate = false;
            _logger.LogInformation("Password rotated for subject {Subject}.", username);
        }

        var (access, refresh, expiresIn) = _tokens.MintTokens(username, mustRotate, roles);
        var response = new AuthenticateResponse
        {
            Allowed = true,
            ExternalSubject = username,
            EnsureUser = true,
            BindingPayload = ByteString.CopyFrom(
                BesPasswordService.BuildBindingPayloadBytes(passwordHash, roles, mustRotate)),
            AccessToken = access,
            RefreshToken = refresh,
            TokenType = "Bearer",
            ExpiresIn = expiresIn,
            MustRotateCredentials = mustRotate,
        };
        response.Roles.AddRange(roles);
        return Task.FromResult(response);
    }

    public override Task<RefreshResponse> Refresh(RefreshRequest request, ServerCallContext context)
    {
        if (!MatchesProviderId(request.ProviderId))
        {
            return Task.FromResult(new RefreshResponse { Allowed = false });
        }

        var (ok, subject, mustRotate, roles) = _tokens.TryValidateRefresh(request.RefreshToken);
        if (!ok || string.IsNullOrWhiteSpace(subject))
        {
            return Task.FromResult(new RefreshResponse { Allowed = false });
        }

        // AUTH-ROLE-001: remint with roles carried on the refresh token (not hardcoded admin).
        var effectiveRoles = roles.Count > 0 ? roles : DefaultRoles;
        var (access, refresh, expiresIn) = _tokens.MintTokens(subject, mustRotate, effectiveRoles);
        return Task.FromResult(new RefreshResponse
        {
            Allowed = true,
            AccessToken = access,
            RefreshToken = refresh,
            TokenType = "Bearer",
            ExpiresIn = expiresIn,
        });
    }

    public override Task<SeedAdminResponse> SeedAdmin(SeedAdminRequest request, ServerCallContext context)
    {
        // mTLS on the work port already requires a mesh CA client cert (Kithara host identity).
        var username = "admin";
        var password = BesPasswordService.GenerateRandomPassword();
        var hash = _passwords.HashPassword(password);
        var welcome = new StringBuilder()
            .Append("Bes seedAdmin created local admin '")
            .Append(username)
            .Append("' with one-time password: ")
            .Append(password)
            .Append(". Change it on first login (must_rotate_credentials; send new_password).")
            .ToString();

        _logger.LogInformation("SeedAdmin created subject {Subject} (password only in welcome text for Kithara logs).", username);

        var response = new SeedAdminResponse
        {
            Created = true,
            WelcomeLogText = welcome,
            ExternalSubject = username,
            BindingPayload = ByteString.CopyFrom(
                BesPasswordService.BuildBindingPayloadBytes(hash, AdminRoles, mustRotate: true)),
            EnsureUser = true,
            MustRotateCredentials = true,
        };
        response.Roles.AddRange(AdminRoles);
        return Task.FromResult(response);
    }
}
