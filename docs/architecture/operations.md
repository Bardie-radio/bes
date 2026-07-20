# Operations

Env and runtime knobs for the **Bes** container.

| Variable | Role |
|----------|------|
| `KITHARA_GRPC_ADDRESS` | Internal DNS to Kithara `:5000` |
| Join secret | Match Kithara `BARDIE_JOIN_SECRETS` for slug `bes` |
| `MODULE_SLUG_OVERRIDE` | Optional |
| JWT signing / TTL | Access + refresh lifetime (Bes-owned) |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | External collector |
| `BARDIE_HTTP_ENABLED` | Sketch — optional HTTP on the adapter. External hosts normally embed the **auth module orchestrator** library. See [03-optional-http](03-optional-http.md) |

Bardie mode: no public ports. No Bes-side user DB volume — persistence is Kithara’s (or the host app’s when HTTP is used outside Bardie).

## Observability

- `service.name=bardie.auth.bes`
- Propagate `traceparent` on RPCs

**Related:** Kithara [configuration](https://github.com/Bardie-radio/kithara/blob/main/docs/architecture/operations/configuration.md) · [observability](https://github.com/Bardie-radio/kithara/blob/main/docs/architecture/operations/observability.md)

**Read next:** [ideas.md](ideas.md)
