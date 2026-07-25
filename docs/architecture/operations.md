# Operations

Env and runtime knobs for the **Bes** container.

| Variable | Role |
|----------|------|
| `KITHARA_GRPC_ADDRESS` | Bardie Compose alias → Logos Channel `HostGrpcAddress` via `Bardie.Logos.Hosting` (scheme optional; defaults to `https://`) |
| `MODULE_HOST_GRPC_ADDRESS` | Generic Module.Channel dial target (used when not on Bardie Compose) |
| `JOIN_SECRET` / `BARDIE_JOIN_SECRET` | Match Kithara `BARDIE_JOIN_SECRETS` for slug `bes` |
| `GRPC_ADVERTISE_ADDRESS` | Where Kithara dials Bes work gRPC (e.g. `dns:///bes:5001` or `https://bes:5001`) |
| `MODULE_SLUG_OVERRIDE` | Optional override of manifest slug |
| `MODULE_MANIFEST_PATH` | Optional path to `module.manifest.json` |
| `MODULE_TLS_DATA_PATH` / `BARDIE_GRPC_TLS_DATA_PATH` | Persist mesh CA + client + work-port server PEMs |
| `MODULE_WORK_GRPC_PORT` / `BARDIE_WORK_GRPC_PORT` | Work gRPC listen port (default `5001`) |
| JWT signing / TTL | Access + refresh lifetime (Bes-owned; not wired in skeleton) |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | External collector |
| `BARDIE_HTTP_ENABLED` | Sketch — optional HTTP on the adapter. External hosts normally embed the **auth module orchestrator** library. See [03-optional-http](03-optional-http.md) |

Bardie mode: no public ports for auth ceremony. HTTP `:8080` is health only. No Bes-side user DB volume — persistence is Kithara’s (or the host app’s when HTTP is used outside Bardie).

Static identity (`slug`, `kind`, `capabilities`, `otelServiceName`) lives in **`module.manifest.json`** only. ModuleChannel options stay host-agnostic; Bes maps `KITHARA_*` / `BARDIE_*` Compose names via **`Bardie.Logos.Hosting`** (`BardieComposeParticipantEnv`). JWT minting and JWKS Register attach via **`Bardie.Module.Auth`**.

## Observability

- `service.name` from manifest (`bardie.auth.bes`)
- Propagate `traceparent` on RPCs

**Related:** Kithara [configuration](https://github.com/Bardie-radio/kithara/blob/main/docs/architecture/operations/configuration.md) · [observability](https://github.com/Bardie-radio/kithara/blob/main/docs/architecture/operations/observability.md) · [module-channel](https://github.com/Bardie-radio/kithara/blob/main/docs/architecture/operations/module-channel.md)

**Read next:** [ideas.md](ideas.md)
