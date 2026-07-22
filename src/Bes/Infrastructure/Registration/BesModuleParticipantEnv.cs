using Bardie.Module.Channel.Participant;
using Microsoft.Extensions.Options;

namespace Bes.Infrastructure.Registration;

/// <summary>
/// Maps Bardie Compose env names onto generic ModuleChannel participant options.
/// ModuleChannel itself does not know about <c>KITHARA_*</c> / <c>BARDIE_*</c> aliases.
/// </summary>
public static class BesModuleParticipantEnv
{
    public static void ApplyBardieComposeAliases(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.PostConfigure<ModuleParticipantOptions>(options =>
        {
            var host = configuration["KITHARA_GRPC_ADDRESS"]
                ?? configuration["MODULE_HOST_GRPC_ADDRESS"];
            if (!string.IsNullOrWhiteSpace(host))
            {
                options.HostGrpcAddress =
                    ModuleParticipantServiceCollectionExtensions.NormalizeGrpcAddress(host);
            }

            var join = configuration["BARDIE_JOIN_SECRET"] ?? configuration["JOIN_SECRET"];
            if (!string.IsNullOrWhiteSpace(join))
            {
                options.JoinSecret = join;
            }

            var advertise = configuration["BARDIE_GRPC_ADVERTISE_ADDRESS"]
                ?? configuration["GRPC_ADVERTISE_ADDRESS"];
            if (!string.IsNullOrWhiteSpace(advertise))
            {
                options.GrpcAdvertiseAddress = advertise;
            }

            var tls = configuration["BARDIE_GRPC_TLS_DATA_PATH"]
                ?? configuration["MODULE_TLS_DATA_PATH"];
            if (!string.IsNullOrWhiteSpace(tls))
            {
                options.TlsDataPath = tls;
            }

            var workPort = configuration["BARDIE_WORK_GRPC_PORT"]
                ?? configuration["MODULE_WORK_GRPC_PORT"];
            if (int.TryParse(workPort, out var port) && port > 0)
            {
                options.WorkGrpcPort = port;
            }
        });
    }
}
