# Bes

Login + password **auth adapter** for Bardie — mints JWTs; Kithara stores users/bindings and verifies via JWKS.

| | |
|--|--|
| **Status** | **MVP Phase 2 complete** — auth adapter live beside Kithara |
| **Image / Compose** | `bes` |
| **OTel** | `bardie.auth.bes` (from `module.manifest.json`) |
| **Slug** | `bes` |
| **Discovery** | `form_schema` (username/password) |

Architecture: [docs/architecture](docs/architecture/README.md).

## Layout

```text
src/Bes/                 ASP.NET host (gRPC AuthAdapter + ModuleChannel participant)
  module.manifest.json   Static Register identity + OTel name
Dockerfile               Multi-stage; build from parent dir with sibling kithara/
Directory.Build.props    ProjectReference ↔ PackageReference hybrid
```

## Libs

When `../kithara/libs` exists (multi-root workspace), Bes uses **ProjectReference** to `Bardie.Contracts` + `Bardie.Module.Channel`. Otherwise **PackageReference** to the published `0.1.0` packages — no proto copies.

```bash
dotnet run --project src/Bes
# Docker (from parent of bes/ + kithara/):
docker build -f bes/Dockerfile -t bes .
```

Kithara contracts: [grpc-auth-adapter](https://github.com/Bardie-radio/kithara/blob/main/docs/architecture/interfaces/grpc-auth-adapter.md) · [auth-adapters](https://github.com/Bardie-radio/kithara/blob/main/docs/architecture/domains/auth-adapters.md) · [auth](https://github.com/Bardie-radio/kithara/blob/main/docs/architecture/interfaces/auth.md) · [ADR 007](https://github.com/Bardie-radio/kithara/blob/main/docs/architecture/adrs/007-auth-adapter-modules.md)

Org: [Bardie architecture](https://github.com/Bardie-radio/.github/tree/main/profile/docs/architecture)
