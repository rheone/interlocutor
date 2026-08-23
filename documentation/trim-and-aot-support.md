# Trim & AOT Support

> [!IMPORTANT]
> **Interlocutor is trim-compatible. It is not currently compatible with ahead-of-time compilation.**
> Every shipping package is marked trimmable and builds clean under trim analysis, and a trimmed
> application dispatches correctly. Publishing ahead-of-time compiled does **not** work today.

> [!WARNING]
> The reason is structural, not an oversight. A response type is a `readonly union` - a value type - and
> pipeline behaviors are registered as open generics closed over that response type by the container at
> dispatch time. Value-type generic instantiations are not shared the way reference-type ones are, so
> closing `IPipelineBehavior<TRequest, TResponse>` over a union needs a just-in-time compiler. A trimmed
> application still has one; an ahead-of-time compiled application does not. This affects every container,
> not one in particular.

> [!NOTE]
> **TBD**: The remaining sections below are unwritten pending a decision on the support level to commit to.
> Write them from actual implemented behavior, not speculation.

## Expected content (once unblocked)

- Trim-safety guarantees Interlocutor makes
- AOT compatibility requirements and opt-ins
- Cost of the runtime-scanning fallback
- Metadata annotations and preservation
- Publishing trimmed/AOT applications
- Known limitations and workarounds

## Links

- [Handler Selection & Dispatch](handler-selection-and-dispatch.md) - compile-time discovery, which removes reflection from the dispatch path and is what makes trimming safe
