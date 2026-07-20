# Contracts

Bes speaks the unified auth-adapter gRPC contract — [grpc-auth-adapter](https://github.com/Bardie-radio/kithara/blob/main/docs/architecture/interfaces/grpc-auth-adapter.md). Only `GetProviders` / `Authenticate` / `Refresh` (plus Register/Health). No protocol-specific RPCs.

## Internal shape (MVP seam)

Implement work verbs as **commands** (handlers) behind a small interface. The gRPC server is a **command surface** (façade). Hosts reach Bes through the **auth module orchestrator** library (Kithara today; external apps later) — keep ensure-user / binding persistence as a **host port**, not logic inside the gRPC class. Optional adapter HTTP later: [03-optional-http](03-optional-http.md).

## Registration

Module Registry `Register` (dial Kithara) with slug `bes`, **join secret**, JWKS (or JWKS URI), capability **`seedAdmin`**. gRPC stays internal.

## GetProviders

Return one provider: `id=bes` (opaque routing handle), `ui.form_schema` with typed fields (e.g. username/password). Clients render the field list without knowing Bes by name; Bes does not host pages.

## Authenticate

Opaque payload (typically username + password from Kithara’s `/api/auth/authenticate`):

1. Verify password against binding payload Kithara holds (or first-login path that asks `ensure_user` + store hash).
2. On success: `allowed`, roles/entities as needed, `access_token` + `refresh_token` (**minted by Bes**), `ensure_user` / `binding_payload` when Kithara should persist.
3. Honor `must_rotate_credentials` for seeded admins (and any forced rotation).

## Refresh

Bes owns refresh for its tokens — validate refresh, mint new access (+ rotate refresh if designed that way). Kithara proxies `POST /api/auth/refresh` → Bes `Refresh`.

## SeedAdmin

When Kithara calls `SeedAdmin` (empty DB): create admin with random secret, return welcome log text + binding material. Only accept calls authenticated as Kithara. See [kithara auth adapter contract](https://github.com/Bardie-radio/kithara/blob/main/docs/architecture/interfaces/grpc-auth-adapter.md).

## Binding payload (typical)

Password hash, reset metadata — stored in Kithara `UserAuthBinding.payload` for provider slug `bes`. Bes has **no** separate auth DB.

**Read next:** [mvp/v0.1-scope.md](mvp/v0.1-scope.md)
