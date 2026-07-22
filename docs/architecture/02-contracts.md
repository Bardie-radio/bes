# Contracts

Bes speaks the unified auth-adapter gRPC contract — [grpc-auth-adapter](https://github.com/Bardie-radio/kithara/blob/main/docs/architecture/interfaces/grpc-auth-adapter.md). Only `GetProviders` / `Authenticate` / `Refresh` (plus Register/Health). No protocol-specific RPCs.

## Internal shape (MVP seam)

Implement work verbs as **commands** (handlers) behind a small interface. The gRPC server is a **command surface** (façade). Hosts reach Bes through the **auth module orchestrator** library (Kithara today; external apps later) — keep ensure-user / binding persistence as a **host port**, not logic inside the gRPC class. Optional adapter HTTP later: [03-optional-http](03-optional-http.md).

## Registration

Module Registry `Register` (dial Kithara) uses generic **`module.manifest.json`** identity (slug `bes`, kind `auth`, capability **`seedAdmin`**) plus env overlays (join secret, advertise address). Bes attaches runtime JWKS via **`Bardie.Module.Auth`** (`AuthJwksRegisterRequestCustomizer`) — not via typed auth bags on the shared `Bardie.Module.Channel` manifest.

MVP advertises **`seedAdmin` only**. Reserved for later (do not advertise until implemented): `selfRegister`, `passwordReset` — see [kithara grpc-auth-adapter](https://github.com/Bardie-radio/kithara/blob/main/docs/architecture/interfaces/grpc-auth-adapter.md) and [module-channel capabilities](https://github.com/Bardie-radio/kithara/blob/main/docs/architecture/operations/module-channel.md). Account linking is Kithara-owned, not a Bes capability.

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
