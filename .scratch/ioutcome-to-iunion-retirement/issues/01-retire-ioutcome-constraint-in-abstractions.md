# 01 — Retire `IOutcome` as the response constraint across `Interlocutor.Abstractions`

**What to build:** every `where TResponse : IOutcome` / `where TResult : IOutcome` constraint inside `Interlocutor.Abstractions` becomes `where TResponse : System.Runtime.CompilerServices.IUnion` / `where TResult : IUnion`. Same compile-time guarantee - a type not marked `[Union]` (and not hand-implementing `IUnion`) still fails to compile against the constraint - now backed by the BCL's own union-identity interface instead of this project's own marker. `IOutcome` and `IOutcomeCase` themselves are not touched, deleted, or renamed by this ticket - only which interface constrains a response.

**Blocked by:** None — can start immediately.

**Status:** ready-for-agent

- [ ] Every constraint site in `Interlocutor.Abstractions` currently reading `where TResponse : IOutcome` or `where TResult : IOutcome` - across the mediator core (`Mediator`, `ISender`), handler contracts (`IQuery`, `ICommand`, `IQueryHandler`, `ICommandHandler`, `IVoidCommandHandler`, their wrappers), every pipeline behavior (`AuthorizationBehavior`, `CancellationBehavior`, `TimeoutBehavior`, `RateLimitedBehavior`, `PipelineChainExecutor`), `Eject`, `NoCommit`, `PipelineEjectException`, and `IOutcomeCaseRegistry`/`OutcomeCaseRegistryBuilder`/`DefaultOutcomeCaseRegistry`/`OutcomeCaseExtractor`/`OutcomeCaseRegistryExtensions` - reads `IUnion` instead.
- [ ] `IOutcome` and `IOutcomeCase` remain in `Interlocutor.CommonKernel.ResultTypes`, unchanged in shape; the generator's existing behavior (a union declaring `: IOutcome` gets the `IOutcomeCase`/`ActiveCaseType` classification surface, one that doesn't gets only the sum-type surface) is untouched.
- [ ] `DefaultOutcomeCaseRegistry.TryGetActiveCase`'s existing `response is IUnion` runtime check and its `OutcomeCaseNotSupportedException` throw path are unaffected in behavior - the constraint change converts what was previously a possible runtime throw (a type satisfying `IOutcome` but not `IUnion`) into a compile-time error for the same misconfiguration.
- [ ] A test proves a response type that is `[Union]`-attributed (or a hand-written `IUnion` implementer) but does **not** implement `IOutcome` still compiles and dispatches correctly through every pipeline behavior in this package.
- [ ] A compile-time regression test (or documented failing-build sample, following this generator's existing diagnostic test conventions) proves a response type implementing bare `IOutcome` but not `IUnion` now fails to compile at these constraint sites.
- [ ] Every existing `Interlocutor.Abstractions.Tests` test continues to pass unmodified in behavior.
- [ ] `dotnet build` and `dotnet test` pass for `Interlocutor.Abstractions`; `dotnet csharpier .` produces no diff.
