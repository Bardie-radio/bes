# Ideas inbox

Parking lot for Bes notes (password reset flows, rate limits, argon2 params, …). Promote when locked.

**Promoted (planned post-MVP):** hosts embed **auth module orchestrator** library (same as Kithara); adapter HTTP secondary — [03-optional-http.md](03-optional-http.md).

**Ops:** Bes final image is Alpine (`aspnet:10.0-alpine3.22`) — META-OPS-002 / [bes#10](https://github.com/Bardie-radio/bes/issues/10) / [kithara#33](https://github.com/Bardie-radio/kithara/issues/33).

**Reserved Registry capabilities (not MVP):** `selfRegister` (open signup via `Authenticate` without operator seed) and `passwordReset` (reset ceremony in the opaque auth bag). Advertise on `module.manifest.json` only when implemented. Account linking stays on Kithara — do not invent an `accountLink` cap.
