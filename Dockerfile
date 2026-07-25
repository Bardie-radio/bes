# Build from the parent folder that contains `bes/`, `logos/`, and `kithara-logos-auth/`
# (multi-root / Local Compose sibling layout → ProjectReference):
#
#   docker build -f bes/Dockerfile -t bes .
#
# Standalone Bes-only builds need published Bardie.Logos.* / Bardie.Module.Auth nupkgs
# (PackageReference when sibling Logos checkouts are absent).
#
# META-OPS-002: Alpine final (busybox wget healthcheck — no curl).
# Build on Debian SDK so Grpc.Tools protoc (glibc) runs; publish for linux-musl-x64.
#
# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY logos/Directory.Build.props logos/Directory.Packages.props logos/
COPY logos/src/Bardie.Logos.Contracts logos/src/Bardie.Logos.Contracts/
COPY logos/src/Bardie.Logos.Channel logos/src/Bardie.Logos.Channel/
COPY logos/src/Bardie.Logos.Hosting logos/src/Bardie.Logos.Hosting/

COPY kithara-logos-auth/Directory.Build.props kithara-logos-auth/Directory.Packages.props kithara-logos-auth/
COPY kithara-logos-auth/src/Bardie.Module.Auth kithara-logos-auth/src/Bardie.Module.Auth/

COPY bes/Directory.Build.props bes/Directory.Packages.props bes/
COPY bes/src/Bes/Bes.csproj bes/src/Bes/
RUN dotnet restore bes/src/Bes/Bes.csproj -r linux-musl-x64

COPY bes/src/Bes/ bes/src/Bes/
# Allow publish to restore again — --no-restore breaks when transitive packages
# (e.g. Google.Protobuf via Bardie.Logos.Contracts) were incomplete in the restore layer.
RUN dotnet publish bes/src/Bes/Bes.csproj \
      -c Release -r linux-musl-x64 --self-contained false \
      -o /app/publish

# Pin alpine3.22 with Kithara/Magpie (floating `10.0-alpine` → 3.23+).
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine3.22 AS final
WORKDIR /app

RUN mkdir -p /data/mtls \
    && chown -R "$APP_UID":"$APP_UID" /data /app

COPY --from=build /app/publish .
RUN chown -R "$APP_UID":"$APP_UID" /app

USER $APP_UID
ENV ASPNETCORE_URLS= \
    MODULE_TLS_DATA_PATH=/data/mtls \
    MODULE_WORK_GRPC_PORT=5001

EXPOSE 8080 5001
HEALTHCHECK --interval=30s --timeout=3s --start-period=20s --retries=3 \
  CMD wget -q -O /dev/null http://127.0.0.1:8080/healthz || exit 1

ENTRYPOINT ["dotnet", "Bes.dll"]
