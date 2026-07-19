# Bes

Login + password **auth adapter** for Bardie — mints JWTs; Kithara stores users/bindings and verifies via JWKS.

| | |
|--|--|
| **Status** | **MVP** v0.1 (WIP — docs first) |
| **Image / Compose** | `bes` |
| **OTel** | `bardie.auth.bes` |
| **Slug** | `bes` |
| **Discovery** | `form_schema` |

Architecture: [docs/architecture](docs/architecture/README.md).

Kithara contracts: [grpc-auth-adapter](https://github.com/Bardie-radio/kithara/blob/main/docs/architecture/interfaces/grpc-auth-adapter.md) · [auth-adapters](https://github.com/Bardie-radio/kithara/blob/main/docs/architecture/domains/auth-adapters.md) · [auth](https://github.com/Bardie-radio/kithara/blob/main/docs/architecture/interfaces/auth.md) · [ADR 007](https://github.com/Bardie-radio/kithara/blob/main/docs/architecture/adrs/007-auth-adapter-modules.md)

Org: [Bardie architecture](https://github.com/Bardie-radio/.github/tree/main/profile/docs/architecture)
