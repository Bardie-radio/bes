# Build from the parent folder that contains both `bes/` and `kithara/`
# (multi-root / Local Compose sibling layout → ProjectReference):
#
#   docker build -f bes/Dockerfile -t bes .
#
# Standalone Bes-only builds need published Bardie.* nupkgs on a NuGet feed
# (PackageReference when ../kithara/libs is absent).

# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY kithara/Directory.Build.props kithara/Directory.Packages.props kithara/
COPY kithara/libs/Bardie.Contracts kithara/libs/Bardie.Contracts/
COPY kithara/libs/Bardie.ModuleChannel kithara/libs/Bardie.ModuleChannel/

COPY bes/Directory.Build.props bes/Directory.Packages.props bes/
COPY bes/src/Bes/Bes.csproj bes/src/Bes/
RUN dotnet restore bes/src/Bes/Bes.csproj

COPY bes/src/Bes/ bes/src/Bes/
RUN dotnet publish bes/src/Bes/Bes.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/ \
    && groupadd --gid 1000 bes \
    && useradd --uid 1000 --gid bes --create-home bes

COPY --from=build /app/publish .
RUN mkdir -p /data/mtls && chown -R bes:bes /data /app

USER bes
ENV ASPNETCORE_URLS= \
    MODULE_TLS_DATA_PATH=/data/mtls \
    MODULE_WORK_GRPC_PORT=5001

EXPOSE 8080 5001
HEALTHCHECK --interval=30s --timeout=3s --start-period=20s --retries=3 \
  CMD curl -fsS http://127.0.0.1:8080/healthz || exit 1

ENTRYPOINT ["dotnet", "Bes.dll"]
