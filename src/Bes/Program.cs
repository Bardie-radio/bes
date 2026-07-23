using Bardie.Module.Auth;
using Bardie.Module.Channel.Participant;
using Bardie.Module.Hosting;
using Bes.Features.Auth;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables();

var manifest = builder.AddBardieModuleHosting(
    configure: options =>
    {
        options.ServerDnsNames = ["bes", "localhost"];
        options.ExpectedHostClientIdentity = "kithara";
    },
    otelFallbackServiceName: "bardie.auth.bes");

builder.Services.AddAuthModuleJwt(builder.Configuration);
builder.Services.PostConfigure<AuthModuleJwtOptions>(options =>
{
    var access = builder.Configuration["BES_JWT_ACCESS_TTL_MINUTES"];
    if (int.TryParse(access, out var minutes) && minutes > 0)
    {
        options.AccessTokenMinutes = minutes;
    }

    var refresh = builder.Configuration["BES_JWT_REFRESH_TTL_DAYS"];
    if (int.TryParse(refresh, out var days) && days > 0)
    {
        options.RefreshTokenDays = days;
    }

    var keyPath = builder.Configuration["BES_JWT_SIGNING_KEY_PATH"];
    if (!string.IsNullOrWhiteSpace(keyPath))
    {
        options.SigningKeyPath = keyPath;
    }

    var audience = builder.Configuration["BES_JWT_AUDIENCE"];
    if (!string.IsNullOrWhiteSpace(audience))
    {
        options.Audience = audience;
    }
});

builder.Services.AddSingleton<BesPasswordService>();
builder.Services.AddGrpc();

var app = builder.Build();

await app.EnsureModuleParticipantServerCertificateAsync().ConfigureAwait(false);

var participantOptions = app.Services.GetRequiredService<IOptions<ModuleParticipantOptions>>().Value;
var httpPort = ModuleHostingPorts.ResolveHttpPort(builder.Configuration);

app.Logger.LogInformation(
    "Bes starting as {Slug} ({Otel}); health HTTP :{HttpPort}; work gRPC :{Port}; host={Host}",
    manifest.Slug,
    manifest.OtelServiceName,
    httpPort,
    participantOptions.WorkGrpcPort,
    participantOptions.HostGrpcAddress);

app.MapGrpcService<AuthAdapterService>();
app.MapModuleHostingEndpoints();

app.Run();

public partial class Program;
