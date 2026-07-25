# Contracts

Bes speaks the unified auth-adapter gRPC contract — [grpc-auth-adapter](https://github.com/Bardie-radio/kithara/blob/main/docs/architecture/interfaces/grpc-auth-adapter.md). Work verbs: `GetProviders` / `Authenticate` / `Refresh` / `UpdateUserBinding` (plus Register/Health). No protocol-specific RPCs. **No `SeedAdminBinding`** — bootstrap and admin provision are host invite OTP (AUTH-INVITE).

## Internal shape (MVP seam)

Implement work verbs as **commands** (handlers) behind a small interface. The gRPC server is a **command surface** (façade). Hosts reach Bes through the **auth module harness** library (Kithara today; external apps later) — keep ensure-user / binding persistence as a **host port**, not logic inside the gRPC class. Optional adapter HTTP later: [03-optional-http](03-optional-http.md).

## Registration

Module Registry `Register` (dial Kithara) uses generic **`module.manifest.json`** identity (slug `bes`, kind `auth`, capability **`updateBinding`**) plus env overlays (join secret, advertise address). Bes attaches runtime JWKS via **`Bardie.Module.Auth`** (`AuthJwksRegisterRequestCustomizer`) — not via typed auth bags on the shared `Bardie.Module.Channel` manifest.

MVP advertises **`updateBinding`** only. Reserved for later (do not advertise until implemented): `selfRegister`, `passwordReset` — see [kithara grpc-auth-adapter](https://github.com/Bardie-radio/kithara/blob/main/docs/architecture/interfaces/grpc-auth-adapter.md) and [module-channel capabilities](https://github.com/Bardie-radio/kithara/blob/main/docs/architecture/operations/module-channel.md). Account linking is Kithara-owned, not a Bes capability.

## GetProviders

Return one provider: `id=bes` (opaque routing handle), `ui.login_form` (username/password) plus `bind_form` (password only). Clients render the field lists without knowing Bes by name; Bes does not host pages.

## Authenticate

Opaque `login_form` bag (typically username + password from Kithara’s `/api/auth/authenticate`):

1. Verify password against binding payload Kithara holds.
2. On success: `allowed`, roles/entities as needed, `access_token` + `refresh_token` (**minted by Bes**), `ensure_user` / `binding_payload` when Kithara should persist.
3. Honor `must_rotate_credentials` on the token when the binding still requires **module-signaled** forced rotate — **do not** accept `new_password` here. Invite completion is host **`must_complete_binding`**, not Bes rotate.

## UpdateUserBinding

Same `bind_form` bag for ceremony **`bind`** (invite claim completion / first bind) and **`update`** (password change). For Bes, **`bind_form` is password-only** — login username is the host `User.Username` (Kithara injects it on bind; clients cannot rename the subject via bind_form). **Step-up proof is not part of this RPC** — clients re-authenticate with `Authenticate` / `login_form` (or redirect) before calling `UpdateUserBinding`. Clear `must_rotate` on successful **update** when the module signals forced rotate. Authenticate never mutates bindings.

## Refresh

Bes owns refresh for its tokens — validate refresh, mint new access (+ rotate refresh if designed that way). Kithara proxies `POST /api/auth/refresh` → Bes `Refresh`.

## First admin (Kithara-owned)

When the user DB is empty, **Kithara** invents DEFAULT_ADMIN with a registration OTP (logged once). Operator **`POST /api/auth/claim`** → claim JWT → **`POST /api/auth/bindings/bes`** ceremony **`bind`**. Bes only implements `UpdateUserBinding` for that bind — it does not seed users. See [kithara auth-adapters](https://github.com/Bardie-radio/kithara/blob/main/docs/architecture/domains/auth-adapters.md).

## Binding payload (typical)

Password hash, roles, must-rotate flag — stored in Kithara `UserAuthBinding.payload` for provider slug `bes`. Bes has **no** separate auth DB.

**Read next:** [mvp/v0.1-scope.md](mvp/v0.1-scope.md)
