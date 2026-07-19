# Contracts

Bes speaks the unified auth-adapter gRPC contract — [grpc-auth-adapter](https://github.com/Bardie-radio/kithara/blob/main/docs/architecture/interfaces/grpc-auth-adapter.md). Only `GetProviders` / `Authenticate` / `Refresh` (plus Register/Health). No protocol-specific RPCs.

## Registration

`Register` with slug `bes`, **join secret**, JWKS (or JWKS URI). gRPC stays internal.

## GetProviders

Return one provider: `id=bes`, `ui_mode=form_schema`, field hints for login (+ optional register/reset later). Clients (e.g. Plume) render the form; Bes does not host pages.

## Authenticate

Opaque payload (typically username + password from Kithara’s `/api/auth/authenticate`):

1. Verify password against binding payload Kithara holds (or first-login path that asks `ensure_user` + store hash).
2. On success: `allowed`, roles/entities as needed, `access_token` + `refresh_token` (**minted by Bes**), `ensure_user` / `binding_payload` when Kithara should persist.

## Refresh

Bes owns refresh for its tokens — validate refresh, mint new access (+ rotate refresh if designed that way). Kithara proxies `POST /api/auth/refresh` → Bes `Refresh`.

## Binding payload (typical)

Password hash, reset metadata — stored in Kithara `UserAuthBinding.payload` for provider slug `bes`. Bes has **no** separate auth DB.

**Read next:** [mvp/v0.1-scope.md](mvp/v0.1-scope.md)
