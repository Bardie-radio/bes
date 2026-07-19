# Operations

Env and runtime knobs for the **Bes** container.

| Variable | Role |
|----------|------|
| `KITHARA_GRPC_ADDRESS` | Internal DNS to Kithara `:5000` |
| Join secret | Match Kithara `BARDIE_JOIN_SECRETS` for slug `bes` |
| `MODULE_SLUG_OVERRIDE` | Optional |
| JWT signing / TTL | Access + refresh lifetime (Bes-owned) |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | External collector |

No public ports. No Bes-side user DB volume — persistence is Kithara’s.

## Observability

- `service.name=bardie.auth.bes`
- Propagate `traceparent` on RPCs

**Related:** Kithara [configuration](https://github.com/Bardie-radio/kithara/blob/main/docs/architecture/operations/configuration.md) · [observability](https://github.com/Bardie-radio/kithara/blob/main/docs/architecture/operations/observability.md)

**Read next:** [ideas.md](ideas.md)
