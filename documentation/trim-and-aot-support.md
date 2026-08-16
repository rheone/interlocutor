# Trim & AOT Support

> [!NOTE]
> **TBD**: This section documents trim-safety and ahead-of-time compilation support. It is blocked on ticket 17 (Trim and AOT hardening) — write this doc from the actual implemented behavior once ticket 17 lands, not from speculation.

## Expected content (once unblocked)

- Trim-safety guarantees Interlocutor makes
- AOT compatibility requirements and opt-ins
- Cost of the runtime-scanning fallback
- Metadata annotations and preservation
- Publishing trimmed/AOT applications
- Known limitations and workarounds

## Links

- [ADR-0008](../docs/adr/0008-compile-time-handler-discovery.md) — compile-time discovery (which enables AOT/trim)
- Ticket 17 (Trim and AOT hardening) — implementation in progress
