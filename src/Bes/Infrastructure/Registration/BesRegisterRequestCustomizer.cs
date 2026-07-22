using Bardie.Module.Channel.Manifest;
using Bardie.Module.Channel.Participant;
using Bardie.Modules.V1;
using Bes.Features.Auth;

namespace Bes.Infrastructure.Registration;

/// <summary>Attaches Bes runtime JWKS on Register.</summary>
public sealed class BesRegisterRequestCustomizer : IModuleRegisterRequestCustomizer
{
    private readonly BesJwtTokenService _tokens;

    public BesRegisterRequestCustomizer(BesJwtTokenService tokens)
    {
        _tokens = tokens;
    }

    public void Customize(RegisterRequest request, ModuleManifest manifest)
    {
        request.Auth = new AuthRegisterDetails
        {
            JwksJson = _tokens.ExportJwksJson(),
        };
    }
}
