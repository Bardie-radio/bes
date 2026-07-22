using Bardie.ModuleChannel.Manifest;
using Bardie.ModuleChannel.Participant;
using Bes.Features.Auth;
using Bes.Infrastructure.Observability;
using Bes.Infrastructure.Registration;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables();

var manifestPath = ModuleManifestLoader.ResolvePath(
    builder.Configuration["MODULE_MANIFEST_PATH"] ?? builder.Configuration["ModuleParticipant:ManifestPath"],
    builder.Environment.ContentRootPath);
var manifest = ModuleManifestLoader.ApplyEnvironmentOverlays(
    ModuleManifestLoader.LoadFromFile(manifestPath),
    builder.Configuration);

builder.Services.AddSingleton(manifest);
builder.Services.Configure<BesJwtOptions>(builder.Configuration.GetSection(BesJwtOptions.SectionName));
builder.Services.PostConfigure<BesJwtOptions>(options =>
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
builder.Services.AddSingleton<BesJwtTokenService>();
builder.Services.AddModuleParticipant(
    builder.Configuration,
    configure: options =>
    {
        options.ServerDnsNames = ["bes", "localhost"];
    },
    contentRoot: builder.Environment.ContentRootPath);
BesModuleParticipantEnv.ApplyBardieComposeAliases(builder.Services, builder.Configuration);
builder.Services.AddSingleton<IModuleRegisterRequestCustomizer, BesRegisterRequestCustomizer>();
builder.Services.AddGrpc();
builder.AddBesOpenTelemetry(manifest);

var workPort = ResolveWorkPort(builder.Configuration);
var httpPort = ResolveHttpPort(builder.Configuration);
builder.WebHost.ConfigureKestrel(options =>
    options.ConfigureBardieModuleParticipantListeners(httpPort: httpPort, workGrpcPort: workPort));

var app = builder.Build();

var participantStore = app.Services.GetRequiredService<IModuleParticipantCertificateStore>();
var participantOptions = app.Services.GetRequiredService<IOptions<ModuleParticipantOptions>>().Value;
await participantStore.EnsureServerCertificateAsync(participantOptions.ServerDnsNames).ConfigureAwait(false);

app.Logger.LogInformation(
    "Bes starting as {Slug} ({Otel}); health HTTP :{HttpPort}; work gRPC :{Port}; host={Host}",
    manifest.Slug,
    manifest.OtelServiceName,
    httpPort,
    participantOptions.WorkGrpcPort,
    participantOptions.HostGrpcAddress);

app.MapGrpcService<AuthAdapterService>();
app.MapGet("/healthz", () => Results.Ok(new { ok = true, slug = manifest.Slug }));
app.MapGet("/", () => Results.Ok(new
{
    service = manifest.OtelServiceName,
    slug = manifest.Slug,
    kind = manifest.Kind,
}));

app.Run();

static int ResolveWorkPort(IConfiguration configuration)
{
    var raw = configuration["BARDIE_WORK_GRPC_PORT"]
        ?? configuration["MODULE_WORK_GRPC_PORT"]
        ?? configuration["ModuleParticipant:WorkGrpcPort"];
    return int.TryParse(raw, out var port) && port > 0 ? port : 5001;
}

static int ResolveHttpPort(IConfiguration configuration)
{
    var raw = configuration["BARDIE_HTTP_PORT"]
        ?? configuration["MODULE_HTTP_PORT"]
        ?? configuration["ModuleParticipant:HttpPort"];
    return int.TryParse(raw, out var port) && port > 0 ? port : 8080;
}

public partial class Program;
