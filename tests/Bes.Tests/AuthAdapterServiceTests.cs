using Bardie.Auth.V1;
using Bardie.Module.Auth;
using Bardie.Logos.Channel.Manifest;
using Bes.Features.Auth;
using Google.Protobuf;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bes.Tests;

/// <summary>META-QA-001 (Bes): Authenticate login-only + UpdateUserBinding bind/update.</summary>
public class AuthAdapterServiceTests
{
    private static (AuthAdapterService Svc, AuthModuleJwtService Tokens) CreateSut()
    {
        var manifest = ModuleManifestLoader.LoadFromJson("""
            {
              "slug": "bes",
              "kind": "auth",
              "displayName": "Bes",
              "otelServiceName": "bardie.auth.bes",
              "capabilities": ["updateBinding"],
              "auth": {
                "loginFormFields": [
                  { "name": "username", "label": "Username", "inputType": "text", "required": true },
                  { "name": "password", "label": "Password", "inputType": "password", "required": true }
                ],
                "bindFormFields": [
                  { "name": "password", "label": "Password", "inputType": "password", "required": true }
                ]
              }
            }
            """);
        var keyDir = Path.Combine(Path.GetTempPath(), "bes-jwt-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(keyDir);
        var tokens = new AuthModuleJwtService(
            Options.Create(new AuthModuleJwtOptions
            {
                SigningKeyPath = Path.Combine(keyDir, "jwt.pem"),
            }),
            manifest);
        var svc = new AuthAdapterService(
            manifest,
            new BesPasswordService(),
            tokens,
            NullLogger<AuthAdapterService>.Instance);
        return (svc, tokens);
    }

    [Fact]
    public async Task Authenticate_rejects_without_binding()
    {
        var (svc, _) = CreateSut();
        var request = new AuthenticateRequest { ProviderId = "bes" };
        request.Payload["username"] = "alice";
        request.Payload["password"] = "password123";

        var response = await svc.Authenticate(request, context: null!);
        Assert.False(response.Allowed);
    }

    [Fact]
    public async Task Bind_then_Authenticate_succeeds()
    {
        var (svc, _) = CreateSut();
        var bind = new UpdateUserBindingRequest
        {
            ProviderId = "bes",
            UserId = Guid.NewGuid().ToString("D"),
            Ceremony = BindingCeremony.Bind,
        };
        bind.Payload["username"] = "alice";
        bind.Payload["password"] = "password123";

        var bound = await svc.UpdateUserBinding(bind, context: null!);
        Assert.True(bound.Ok);
        Assert.False(bound.MustRotateCredentials);
        Assert.Equal("alice", bound.ExternalSubject);
        Assert.False(bound.BindingPayload.IsEmpty);

        var login = new AuthenticateRequest
        {
            ProviderId = "bes",
            ExistingBindingPayload = bound.BindingPayload,
        };
        login.Payload["username"] = "alice";
        login.Payload["password"] = "password123";

        var auth = await svc.Authenticate(login, context: null!);
        Assert.True(auth.Allowed);
        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
        Assert.False(auth.MustRotateCredentials);
    }

    [Fact]
    public async Task Authenticate_rejects_wrong_password()
    {
        var (svc, _) = CreateSut();
        var bind = new UpdateUserBindingRequest
        {
            ProviderId = "bes",
            Ceremony = BindingCeremony.Bind,
        };
        bind.Payload["username"] = "bob";
        bind.Payload["password"] = "password123";
        var bound = await svc.UpdateUserBinding(bind, context: null!);

        var login = new AuthenticateRequest
        {
            ProviderId = "bes",
            ExistingBindingPayload = bound.BindingPayload,
        };
        login.Payload["username"] = "bob";
        login.Payload["password"] = "wrong-password";

        var auth = await svc.Authenticate(login, context: null!);
        Assert.False(auth.Allowed);
    }

    [Fact]
    public async Task Update_changes_password_without_must_rotate()
    {
        var (svc, _) = CreateSut();
        var bind = new UpdateUserBindingRequest
        {
            ProviderId = "bes",
            Ceremony = BindingCeremony.Bind,
        };
        bind.Payload["username"] = "carol";
        bind.Payload["password"] = "password123";
        var bound = await svc.UpdateUserBinding(bind, context: null!);

        var update = new UpdateUserBindingRequest
        {
            ProviderId = "bes",
            Ceremony = BindingCeremony.Update,
            ExistingBindingPayload = bound.BindingPayload,
        };
        update.Payload["password"] = "newpassword99";

        var updated = await svc.UpdateUserBinding(update, context: null!);
        Assert.True(updated.Ok);
        Assert.False(updated.MustRotateCredentials);

        var login = new AuthenticateRequest
        {
            ProviderId = "bes",
            ExistingBindingPayload = updated.BindingPayload,
        };
        login.Payload["username"] = "carol";
        login.Payload["password"] = "newpassword99";
        var auth = await svc.Authenticate(login, context: null!);
        Assert.True(auth.Allowed);
    }

    [Fact]
    public async Task Bind_rejects_short_password()
    {
        var (svc, _) = CreateSut();
        var bind = new UpdateUserBindingRequest
        {
            ProviderId = "bes",
            Ceremony = BindingCeremony.Bind,
        };
        bind.Payload["username"] = "dave";
        bind.Payload["password"] = "short";

        var bound = await svc.UpdateUserBinding(bind, context: null!);
        Assert.False(bound.Ok);
        Assert.Contains("8", bound.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetProviders_exposes_login_and_bind_forms()
    {
        var (svc, _) = CreateSut();
        var response = await svc.GetProviders(new GetProvidersRequest(), context: null!);
        Assert.Single(response.Providers);
        var provider = response.Providers[0];
        Assert.Equal("bes", provider.Id);
        Assert.NotNull(provider.LoginForm);
        Assert.NotNull(provider.BindForm);
        Assert.Equal(2, provider.LoginForm.Fields.Count);
        Assert.Single(provider.BindForm.Fields);
    }
}
