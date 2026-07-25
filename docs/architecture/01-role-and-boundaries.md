# Role and boundaries

Bes is the MVP **login + password** provider. Clients never call Bes on the public edge — credentials go to Kithara; Kithara calls Bes over gRPC (BFF).

## Owns

- Password verify (and hash material Bes asks Kithara to store on the binding)
- **Minting** access + refresh JWTs; refresh semantics for Bes-issued tokens
- Publishing **JWKS** at register so Kithara can verify
- `GetProviders` descriptor with `login_form` + `bind_form` (fields for clients to render)
- OTLP as `bardie.auth.bes`
- JWT mint / refresh TTL knobs (on Bes, not Kithara)

## Does not own

- User rows / `UserAuthBinding` tables (Kithara DB — Bes returns `ensure_user` + binding payload)
- Public login HTML (clients render from discovery)
- Guest control JWTs / listen tokens (Kithara)
- OIDC / passkeys (Argus / Hecate)

## Surfaces

| Surface | Audience |
|---------|----------|
| gRPC to Kithara | Internal only (Bardie default) |
| No Bardie public HTTP login | Clients authenticate via Kithara BFF |
| Optional HTTP decorator | Secondary; primary outside path is **auth orch library** in the host — see [03-optional-http](03-optional-http.md) |

**Read next:** [02-contracts.md](02-contracts.md)
