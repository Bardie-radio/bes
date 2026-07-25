using Bardie.Auth.V1;
using Bardie.Module.Auth;
using Bardie.Module.Channel.Manifest;
using Google.Protobuf;
using Grpc.Core;

namespace Bes.Features.Auth;

/// <summary>AuthAdapter gRPC façade — login + binding commands; JWT mint stays in AuthModuleJwtService.</summary>
public sealed class AuthAdapterService : AuthAdapterModuleBase
{
    private static readonly string[] DefaultRoles = ["user"];
    private const int MinPasswordLength = 8;

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
        var loginForm = ModuleManifestAuthBag.TryBuildLoginForm(Manifest)
            ?? throw new InvalidOperationException(
                "Bes module.manifest.json must declare auth.loginFormFields (or legacy auth.formFields) for GetProviders.");
        var bindForm = ModuleManifestAuthBag.TryBuildBindForm(Manifest)
            ?? throw new InvalidOperationException(
                "Bes module.manifest.json must declare auth.bindFormFields for GetProviders.");

        var response = new GetProvidersResponse();
        response.Providers.Add(new ProviderDescriptor
        {
            Id = Manifest.Slug,
            DisplayName = string.IsNullOrWhiteSpace(Manifest.DisplayName) ? "Bes" : Manifest.DisplayName,
            LoginForm = loginForm,
            BindForm = bindForm,
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
        username = username?.Trim();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return Task.FromResult(Denied());
        }

        var binding = BesPasswordService.TryReadBinding(request.ExistingBindingPayload.Span);
        if (binding is null)
        {
            // No binding yet — login requires UpdateUserBinding ceremony bind (claim or selfRegister).
            _logger.LogInformation("Authenticate rejected for {User}: no binding payload.", username);
            return Task.FromResult(Denied());
        }

        if (!_passwords.Verify(binding.PasswordHash, password))
        {
            _logger.LogInformation("Authenticate rejected for {User}: bad password.", username);
            return Task.FromResult(Denied());
        }

        // AUTH-ROLE-001: roles from binding; host invite sets admin on claim bind. Missing roles → user.
        var roles = binding.Roles.Count > 0 ? binding.Roles.ToArray() : DefaultRoles;
        var mustRotate = binding.MustRotate;

        // Authenticate is login-only — no new_password / binding mutation (AUTH-ROT via UpdateUserBinding).
        var (access, refresh, expiresIn) = _tokens.MintTokens(username, mustRotate, roles);
        var response = new AuthenticateResponse
        {
            Allowed = true,
            ExternalSubject = username,
            EnsureUser = true,
            BindingPayload = ByteString.CopyFrom(
                BesPasswordService.BuildBindingPayloadBytes(binding.PasswordHash, roles, mustRotate)),
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

    public override Task<UpdateUserBindingResponse> UpdateUserBinding(
        UpdateUserBindingRequest request,
        ServerCallContext context)
    {
        if (!MatchesProviderId(request.ProviderId))
        {
            return Task.FromResult(Rejected("Unknown provider."));
        }

        var ceremony = request.Ceremony;
        if (ceremony == BindingCeremony.Unspecified)
        {
            ceremony = request.ExistingBindingPayload.IsEmpty
                ? BindingCeremony.Bind
                : BindingCeremony.Update;
        }

        return ceremony switch
        {
            BindingCeremony.Bind => Task.FromResult(Bind(request)),
            BindingCeremony.Update => Task.FromResult(Update(request)),
            _ => Task.FromResult(Rejected("Unknown binding ceremony.")),
        };
    }

    private UpdateUserBindingResponse Bind(UpdateUserBindingRequest request)
    {
        if (!request.ExistingBindingPayload.IsEmpty)
        {
            _logger.LogInformation("UpdateUserBinding bind rejected: binding already exists.");
            return Rejected("A binding already exists for this account.");
        }

        // Host injects username (= User.Username) on claim bind; clients must not invent login ids.
        request.Payload.TryGetValue("username", out var username);
        request.Payload.TryGetValue("password", out var password);
        username = username?.Trim();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogInformation("UpdateUserBinding bind rejected: username/password required.");
            return Rejected("Username and password are required.");
        }

        if (password.Length < MinPasswordLength)
        {
            _logger.LogInformation("UpdateUserBinding bind rejected for {User}: password too short.", username);
            return Rejected($"Password must be at least {MinPasswordLength} characters.");
        }

        var hash = _passwords.HashPassword(password);
        var response = new UpdateUserBindingResponse
        {
            Ok = true,
            ExternalSubject = username,
            BindingPayload = ByteString.CopyFrom(
                BesPasswordService.BuildBindingPayloadBytes(hash, DefaultRoles, mustRotate: false)),
            MustRotateCredentials = false,
        };
        response.Roles.AddRange(DefaultRoles);
        return response;
    }

    private UpdateUserBindingResponse Update(UpdateUserBindingRequest request)
    {
        var binding = BesPasswordService.TryReadBinding(request.ExistingBindingPayload.Span);
        if (binding is null)
        {
            _logger.LogInformation("UpdateUserBinding update rejected: no existing binding.");
            return Rejected("No existing credentials to update.");
        }

        // Password-only — login username is host User.Username (immutable via bind_form).
        request.Payload.TryGetValue("password", out var password);
        if (string.IsNullOrWhiteSpace(password))
        {
            _logger.LogInformation("UpdateUserBinding update rejected: empty bind_form bag.");
            return Rejected("No binding fields to update.");
        }

        if (password.Length < MinPasswordLength)
        {
            _logger.LogInformation("UpdateUserBinding update rejected: password too short.");
            return Rejected($"Password must be at least {MinPasswordLength} characters.");
        }

        var roles = binding.Roles.Count > 0 ? binding.Roles.ToArray() : DefaultRoles;
        var hash = _passwords.HashPassword(password);

        // Empty ExternalSubject → host keeps the existing subject (pinned to User.Username).
        var response = new UpdateUserBindingResponse
        {
            Ok = true,
            ExternalSubject = string.Empty,
            BindingPayload = ByteString.CopyFrom(
                BesPasswordService.BuildBindingPayloadBytes(hash, roles, mustRotate: false)),
            MustRotateCredentials = false,
        };
        response.Roles.AddRange(roles);
        _logger.LogInformation("Binding updated via UpdateUserBinding for user {UserId}.", request.UserId);
        return response;
    }

    private static UpdateUserBindingResponse Rejected(string error) => new()
    {
        Ok = false,
        Error = error,
    };
}
