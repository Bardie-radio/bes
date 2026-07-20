# Bes architecture

How Bes implements Bardie’s auth-adapter contract. Kithara owns the user DB and JWT **verification**; Bes owns password proof and JWT **mint** / refresh. Bes may also expose an **optional HTTP** façade for non-Bardie hosts ([03-optional-http](03-optional-http.md)) — Kithara does not define that mode.

## Read order

1. [01-role-and-boundaries.md](01-role-and-boundaries.md)
2. [02-contracts.md](02-contracts.md)
3. [mvp/v0.1-scope.md](mvp/v0.1-scope.md)
4. [operations.md](operations.md)
5. [03-optional-http.md](03-optional-http.md) — optional; outside Bardie
6. [ideas.md](ideas.md)

**Related:** [../README.md](../README.md)
