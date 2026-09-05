# 05 - Tag the Activity when a pipeline dispatch ejects

**What to build:** `QueryHandlerWrapper.cs`, `CommandHandlerWrapper.cs`, `VoidCommandHandlerWrapper.cs`, and the generator's emitted dispatch wrappers each catch `PipelineEjectException<TResponse>` at every pipeline layer and convert it back into an ordinary response, but nothing records that an eject happened at all - post-handler code (an outer behavior, a trace viewer) currently cannot distinguish "the handler returned normally" from "something ejected mid-flight." Reuse the `Activity? activity` parameter already threaded through each wrapper's `Handle` method (already used for `activity?.SetTag(MediatorActivitySource.HandlerTypeTag, ...)`) to tag or add an event to the activity at the point each `catch (PipelineEjectException<TResponse> eject)` block runs, rather than introducing `RequestContextItems` (currently unused anywhere in `src/`) as a first, unprecedented consumer.

**Blocked by:** None - can start immediately

**Status:** ready-for-agent

- [ ] Every wrapper's `catch (PipelineEjectException<TResponse> eject)` block tags or adds an event to the ambient `Activity` (when non-null) recording that an eject occurred
- [ ] The `Outcome`/`IUnion` shape returned to the caller is completely unchanged - this is tracing-only, no behavior or type change
- [ ] Covered by a test asserting the activity carries the new tag/event when a handler or behavior ejects, and does not carry it on a normal return
- [ ] Applied consistently across `QueryHandlerWrapper`, `CommandHandlerWrapper`, `VoidCommandHandlerWrapper`, and whatever the generator emits for the equivalent compile-time dispatch path
