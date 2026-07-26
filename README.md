# Bes

Login + password **auth adapter** for Bardie — mints JWTs; Kithara stores users/bindings and verifies via JWKS.

| | |
|--|--|
| **Status** | **MVP Phase 2 complete** — auth adapter live beside Kithara |
| **Image / Compose** | `bes` |
| **OTel** | `bardie.auth.bes` (from `module.manifest.json`) |
| **Slug** | `bes` |
| **Discovery** | `login_form` + `bind_form` (username/password) |

Architecture: [docs/architecture](docs/architecture/README.md).

## Layout

```text
src/Bes/                 ASP.NET host (gRPC AuthAdapter + Module.* participant)
  module.manifest.json   Static Register identity + OTel name
Dockerfile               Multi-stage; restores Bardie.* from nuget.org
Directory.Packages.props Pins Bardie.Logos.* / Bardie.Module.Auth
```

## Libs

`PackageReference` to nuget.org `Bardie.Logos.Contracts`, `Bardie.Logos.Channel`, `Bardie.Logos.Hosting`, and `Bardie.Module.Auth` (versions in `Directory.Packages.props`) — no proto copies, no sibling ProjectReferences.

```bash
dotnet run --project src/Bes
docker build -t bes .
```

Kithara contracts: [grpc-auth-adapter](https://github.com/Bardie-radio/kithara/blob/main/docs/architecture/interfaces/grpc-auth-adapter.md) · [auth-adapters](https://github.com/Bardie-radio/kithara/blob/main/docs/architecture/domains/auth-adapters.md) · [auth](https://github.com/Bardie-radio/kithara/blob/main/docs/architecture/interfaces/auth.md) · [ADR 007](https://github.com/Bardie-radio/kithara/blob/main/docs/architecture/adrs/007-auth-adapter-modules.md)

Org: [Bardie architecture](https://github.com/Bardie-radio/.github/tree/main/profile/docs/architecture)
