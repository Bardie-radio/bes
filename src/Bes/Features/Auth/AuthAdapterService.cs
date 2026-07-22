using System.Text;
using Bardie.Auth.V1;
using Bardie.Module.Channel.Manifest;
using Google.Protobuf;
using Grpc.Core;

namespace Bes.Features.Auth;

/// <summary>AuthAdapter gRPC façade — commands live in password + JWT services.</summary>
public sealed class AuthAdapterService : AuthAdapter.AuthAdapterBase
{
    private readonly ModuleManifest _manifest;
    private readonly BesPasswordService _passwords;
    private readonly BesJwtTokenService _tokens;
    private readonly ILogger<AuthAdapterService> _logger;

    public AuthAdapterService(
        ModuleManifest manifest,
        BesPasswordService passwords,
        BesJwtTokenService tokens,
        ILogger<AuthAdapterService> logger)
    {
        _manifest = manifest;
        _passwords = passwords;
        _tokens = tokens;
        _logger = logger;
    }

    public override Task<HealthResponse> Health(HealthRequest request, ServerCallContext context) =>
        Task.FromResult(new HealthResponse { Ok = true });

    public override Task<GetProvidersResponse> GetProviders(GetProvidersRequest request, ServerCallContext context)
    {
        var response = new GetProvidersResponse();
        response.Providers.Add(new ProviderDescriptor
        {
            Id = _manifest.Slug,
            DisplayName = string.IsNullOrWhiteSpace(_manifest.DisplayName) ? "Bes" : _manifest.DisplayName,
            FormSchema = new FormSchemaUi
            {
                Fields =
                {
                    new FormField
                    {
                        Name = "username",
                        Label = "Username",
                        InputType = "text",
                        Required = true,
                    },
                    new FormField
                    {
                        Name = "password",
                        Label = "Password",
                        InputType = "password",
                        Required = true,
                    },
                },
            },
        });
        return Task.FromResult(response);
    }

    public override Task<AuthenticateResponse> Authenticate(AuthenticateRequest request, ServerCallContext context)
    {
        if (!string.Equals(request.ProviderId, _manifest.Slug, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(request.ProviderId))
        {
            return Task.FromResult(Denied());
        }

        request.Payload.TryGetValue("username", out var username);
        request.Payload.TryGetValue("password", out var password);
        username = username?.Trim();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return Task.FromResult(Denied());
        }

        var existingHash = BesPasswordService.TryReadPasswordHash(request.ExistingBindingPayload.Span);
        if (existingHash is null)
        {
            // No binding yet — login requires a prior SeedAdmin (or future selfRegister).
            _logger.LogInformation("Authenticate rejected for {User}: no binding payload.", username);
            return Task.FromResult(Denied());
        }

        if (!_passwords.Verify(existingHash, password))
        {
            _logger.LogInformation("Authenticate rejected for {User}: bad password.", username);
            return Task.FromResult(Denied());
        }

        var (access, refresh, expiresIn) = _tokens.MintTokens(username, mustRotateCredentials: false, roles: ["admin"]);
        var response = new AuthenticateResponse
        {
            Allowed = true,
            ExternalSubject = username,
            EnsureUser = true,
            BindingPayload = ByteString.CopyFrom(BesPasswordService.BuildBindingPayloadBytes(existingHash)),
            AccessToken = access,
            RefreshToken = refresh,
            TokenType = "Bearer",
            ExpiresIn = expiresIn,
            MustRotateCredentials = false,
        };
        response.Roles.Add("admin");
        return Task.FromResult(response);
    }

    public override Task<RefreshResponse> Refresh(RefreshRequest request, ServerCallContext context)
    {
        if (!string.Equals(request.ProviderId, _manifest.Slug, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(request.ProviderId))
        {
            return Task.FromResult(new RefreshResponse { Allowed = false });
        }

        var (ok, subject, mustRotate) = _tokens.TryValidateRefresh(request.RefreshToken);
        if (!ok || string.IsNullOrWhiteSpace(subject))
        {
            return Task.FromResult(new RefreshResponse { Allowed = false });
        }

        var (access, refresh, expiresIn) = _tokens.MintTokens(subject, mustRotate, roles: ["admin"]);
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
            .Append(". Change it on first login (must_rotate_credentials).")
            .ToString();

        _logger.LogInformation("SeedAdmin created subject {Subject} (password only in welcome text for Kithara logs).", username);

        var response = new SeedAdminResponse
        {
            Created = true,
            WelcomeLogText = welcome,
            ExternalSubject = username,
            BindingPayload = ByteString.CopyFrom(BesPasswordService.BuildBindingPayloadBytes(hash)),
            EnsureUser = true,
            MustRotateCredentials = true,
        };
        response.Roles.Add("admin");
        return Task.FromResult(response);
    }

    private static AuthenticateResponse Denied() => new()
    {
        Allowed = false,
        TokenType = "Bearer",
    };
}
