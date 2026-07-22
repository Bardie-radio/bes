# Auth adapters and the orchestrator library

**Status: planned (post-MVP) for outside reuse; library shape starts in MVP.**

Bes (and other auth adapters) expose the same **commands** whether the host is Kithara or an external app. Outside users do **not** reimplement discovery/routing — they embed the same **auth module orchestrator** library Kithara uses, and implement its **persistence ports** (user / binding store).

MVP ships Bes as **gRPC only** behind Kithara. Leave seams so HTTP façades and a published orch package land cleanly.

## Who runs what

| Piece | Role |
|-------|------|
| **Auth module orchestrator** (library) | Merge `GetProviders`, route `Authenticate` / `Refresh`, JWKS verify helpers, optional `SeedAdmin`; calls adapters over gRPC (or HTTP later) |
| **Host ports** | Persist user/binding when `ensure_user`; policy knobs. Kithara = its user DB. External app = their store |
| **Bes / Argus / Hecate** | Adapter containers — proof + mint/forward JWT. Command core + gRPC façade (+ optional adapter HTTP later) |

Kithara may still own Bardie-only behaviour **around** the library (guest codes, join secrets, Struna listen checks, REST `/api/auth/*` BFF). That stays out of the shared orch package.

Org packaging: [modules beyond Bardie](https://github.com/Bardie-radio/.github/blob/main/profile/docs/architecture/07-modules-beyond-bardie.md).

## Optional HTTP on the adapter

| Env (sketch) | Default | Effect |
|--------------|---------|--------|
| `BARDIE_HTTP_ENABLED` | unset / `false` | Off → gRPC only |

Primary outside path is **host + auth orch library → adapters**. Adapter HTTP is optional (debug, or hosts that want to call Bes without the library). No separate “standalone write mode” on Bes — verbs stay the same.

## Implementation seam (leave this in MVP)

| Layer | Own | MVP | Later |
|-------|-----|-----|-------|
| **Commands** | `GetProviders`, `Authenticate`, `Refresh`, `SeedAdmin`, … | Yes | Same |
| **Command surfaces** | Façades onto commands | **gRPC** | + optional **HTTP** |
| **Host persistence port** | Ensure user / store binding | Called via Kithara (or orch → Kithara port) | Same port interface for external hosts |

Prefer **Command** handlers; gRPC/HTTP only deserialize and dispatch.

## Security notes

- Do **not** publish Bes on a Bardie public edge — Kithara remains the login BFF there.
- `SeedAdmin` stays privileged (mTLS / equivalent), whether the caller is Kithara or another host using the orch library.

## Out of MVP

Publishing the auth orch package and non-Bardie hosts is **future planned**. Shaping Auth Orchestrator as a **library with host ports** inside Kithara is an MVP obligation so extraction is boring later.

**Related:** [01-role-and-boundaries.md](01-role-and-boundaries.md) · [operations.md](operations.md) · [02-contracts.md](02-contracts.md)

**Read next:** [ideas.md](ideas.md)
