# Build from this repo (or compose context ../bes):
#
#   docker build -t bes .
#
# Restores Bardie.Logos.* / Bardie.Module.Auth from nuget.org.
#
# META-OPS-002: Alpine final (busybox wget healthcheck — no curl).
# Build on Debian SDK so Grpc.Tools protoc (glibc) runs; publish for linux-musl-x64.
#
# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/Bes/Bes.csproj src/Bes/
RUN dotnet restore src/Bes/Bes.csproj -r linux-musl-x64

COPY src/Bes/ src/Bes/
# Allow publish to restore again — --no-restore breaks when transitive packages
# (e.g. Google.Protobuf via Bardie.Logos.Contracts) were incomplete in the restore layer.
RUN dotnet publish src/Bes/Bes.csproj \
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
