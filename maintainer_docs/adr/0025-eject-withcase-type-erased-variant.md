# ADR-0025: `Eject.WithCase<TCase>` - a Type-Erased Case-Eject Variant, Partially Adopted

**Status:** Accepted

## Context

[ADR-0023](0023-pipeline-behavior-case-construction-fail-fast.md) records the fail-fast contract every
case-constructing pipeline behavior must uphold, but not the mechanism those six behaviors used to reach it:
each one - `AuthorizationBehavior` (`Forbidden`), `CancellationBehavior` (`Cancelled`), `TimeoutBehavior` (a
timeout case), `RateLimitedBehavior` (`RateLimited`), `Interlocutor.FluentValidation.ValidationBehavior`
(`ValidationErrors`), and `FluentValidationErrorsBehavior` (`FluentValidationErrors`) - took an
`IOutcomeCaseRegistry` constructor dependency and called
`caseRegistry.ConstructOrThrow<TResponse, TCase>(value, behaviorName, eventPhrase, requirementClause)` at the
one point it short-circuits, passing its own name and phrasing into the exception message
`ConstructOrThrow` throws when `TResponse` can't hold the case.

`.scratch/pipeline-case-fidelity/issues/07-spike-eject-withcase-type-erased.md` spiked an alternative: could a
behavior eject the case value itself, with no `IOutcomeCaseRegistry` dependency of its own, and let the
pipeline's own dispatch machinery - the one place that already knows `TResponse` concretely - do the
construction instead? The spike write-up
(`.scratch/pipeline-case-fidelity/spike-07-blast-radius-writeup.md`) built this for real, migrated
`AuthorizationBehavior` as the worked example, and measured (not estimated) the cost of extending it to the
other five behaviors plus the source generator's emitted dispatch wrappers. Per that issue's own status line,
the resulting "commit or drop" call was made directly - commit - rather than routed through this ticket; this
ADR records that already-made decision and the state it actually shipped in, not a decision made here.

## Decision

### The shape: `Eject.WithCase<TCase>`, generic only over the case

`Eject.WithCase<TCase>(TCase caseValue)` (`src/Interlocutor.Abstractions/Pipeline/Eject.cs`) is generic only in
the case type being ejected, never in the response type it will eventually become. Unlike
`Eject.With<TResponse>(TResponse response)` - which throws internally and is typed to `TResponse` so a call
reads naturally in a `return` statement - `WithCase<TCase>` does **not** throw. It **constructs and returns** a
`PipelineEjectCaseException`, so the call site supplies its own `throw`:

```csharp
throw Eject.WithCase(new Forbidden($"Policy '{request.PolicyName}' denied access to {typeof(TRequest).Name}"));
```

This is a deliberate, load-bearing difference from `Eject.With`'s shape, not an inconsistency: `WithCase<TCase>`'s
only generic parameter (`TCase`, e.g. `Forbidden`) is not assignment-compatible with the enclosing method's
actual `Task<TResponse>` return type, and `throw` requires an already-constructed `Exception` operand - there is
no way to keep `Eject.With`'s internal-throw-typed-to-the-return-type trick once the only type parameter in
scope is the case, not the response.

`PipelineEjectCaseException` (`src/Interlocutor.Abstractions/Pipeline/PipelineEjectCaseException.cs`) is
**non-generic**, not `PipelineEjectCaseException<TCase>`. It carries the case boxed as `object CaseValue` plus
its runtime `Type CaseType` - mirroring `System.Runtime.CompilerServices.IUnion.Value`'s own shape. A generic
`PipelineEjectCaseException<TCase>` was considered and does not work: the shared catch site
(`PipelineChainExecutor.ExecuteAsync<TRequest, TResponse>`) is generic only over `TRequest`/`TResponse`, and
different behaviors eject different case types through the identical two catch clauses that site already
guards - a single non-generic exception type, carrying the case erased to `object` plus its runtime `Type`, is
what one shared catch clause can actually catch regardless of which case was ejected.

### The construction move: from each behavior to one shared catch site

Before this change, every one of the six behaviors constructed its own response inline, at its own
short-circuit point, via `IOutcomeCaseRegistry.ConstructOrThrow<TResponse, TCase>`. After this change (for the
one behavior actually migrated - see "Current shipped scope" below), the behavior no longer holds an
`IOutcomeCaseRegistry` dependency at all; construction moves entirely to
`PipelineChainExecutor.ExecuteAsync<TRequest, TResponse>` (`src/Interlocutor.Abstractions/Pipeline/PipelineChainExecutor.cs`),
which already wrapped both the innermost handler call and every wrapped behavior call in a
`catch (PipelineEjectException<TResponse> eject)` guard and now gained a second, sibling
`catch (PipelineEjectCaseException eject)` guard at both of those layers. That catch site resolves
`IOutcomeCaseRegistry` lazily from the ambient `IServiceProvider` (only when a case-eject actually happens - a
consumer with no case-ejecting behavior registered never resolves it) and calls a new bridging extension,
`OutcomeCaseRegistryExtensions.ConstructOrThrow<TResponse>(IOutcomeCaseRegistry, PipelineEjectCaseException)`
(`src/Interlocutor.Abstractions/Pipeline/Cases/OutcomeCaseRegistryExtensions.cs`), which closes
`IOutcomeCaseRegistry.Construct<TResponse, TCase>` reflectively over the exception's runtime `CaseType` via
`MethodInfo.MakeGenericMethod` - cached per `(TResponse, CaseType)` pair, mirroring the registry's own
constructor-lookup cache - and rewraps a resulting `OutcomeCaseNotSupportedException` into the same kind of
named `InvalidOperationException` the six behaviors already threw themselves, preserving
[ADR-0023](0023-pipeline-behavior-case-construction-fail-fast.md)'s fail-fast guarantee at the new call site.

Because `QueryHandlerWrapper`, `CommandHandlerWrapper`, and `BareCommandHandlerWrapper` all delegate their
chain-build/eject-guard logic entirely to this one shared `PipelineChainExecutor`, adding the second catch
clause there is the entire hand-written-wrapper-side change - it does not vary by wrapper kind or by which
behavior ejected, only by `TResponse`, which the executor is already generic over.

### Current shipped scope: one behavior, not all six

**Only `AuthorizationBehavior<TRequest, TResponse>` is migrated.** It takes no `IOutcomeCaseRegistry` dependency
and throws `Eject.WithCase(new Forbidden(...))` on denial. `CancellationBehavior`, `TimeoutBehavior`,
`RateLimitedBehavior`, `ValidationBehavior`, and `FluentValidationErrorsBehavior` are **unchanged** - each still
takes `IOutcomeCaseRegistry` in its constructor and still calls
`caseRegistry.ConstructOrThrow<TResponse, TCase>(value, behaviorName, eventPhrase, requirementClause)` directly,
exactly as ADR-0023 describes.

The generator-side gap this section originally recorded - `Interlocutor.Generator`'s `DispatchWrapperEmitter`
emitting only a `catch (PipelineEjectException<TResponse> eject)` block, with no corresponding
`PipelineEjectCaseException` clause, so a compile-time-discovery consumer registering `AuthorizationBehavior`
would see the exception propagate unhandled - is now closed, not by mirroring a second generated catch block as
the "Follow-on work" section below originally called for, but by a deeper fix: `DispatchWrapperEmitter` no
longer emits fold/eject-catch logic as string-templated source at all. It now emits a single call into the
same public `PipelineChainExecutor.ExecuteAsync` the hand-written wrappers already called, so both dispatch
paths run through the one implementation and gain any future eject-catch change (this one included) together,
with nothing left to keep in sync by hand.

### The measured blast radius: uniform, mechanical, and already fully sized

The spike measured, rather than estimated, the cost of extending this to the remaining five behaviors: all six
behaviors that short-circuit via `ConstructOrThrow` share the *exact same shape* (an `IOutcomeCaseRegistry`
constructor dependency, one `ConstructOrThrow` call at the single short-circuit point), so migrating each of
the remaining five is drop the dependency, replace the `ConstructOrThrow` call with `throw Eject.WithCase(value)`,
and delete the now-dead private `Construct*Response` helper three of them have. No behavior-specific wrinkle
turned up among the six. Both the hand-written-wrapper side of this work and the generator-emitter side are
fully done regardless of how many behaviors migrate, because `PipelineChainExecutor` is shared by both dispatch
paths and not per-behavior - see "Current shipped scope" above.

### Consistency with ADR-0023 and ADR-0020

This design stays consistent with [ADR-0023](0023-pipeline-behavior-case-construction-fail-fast.md)'s fail-fast
principle rather than weakening it: `Eject.WithCase` still routes through `ConstructOrThrow` - just from
`PipelineChainExecutor`'s catch site instead of from inside the ejecting behavior - and a `TResponse` that can't
hold the ejected case still produces the same kind of named `InvalidOperationException`, never a silent or
reshaped outcome.

This is also a different shape from [ADR-0020](0020-validation-failure-case-fidelity.md)'s already-rejected
"decompose `TResponse` into a fixed failure-case generic parameter on the behavior" alternative (e.g.
`ValidationBehavior<TRequest, TResponse, TFailure>`), and does not reopen that rejection. ADR-0020 rejected
adding a third *open generic parameter to the behavior itself*, which Microsoft.Extensions.DependencyInjection's
open-generic resolver cannot resolve against a two-parameter service interface
(`IPipelineBehavior<TRequest, TResponse>`). `Eject.WithCase<TCase>` adds no generic parameter to any behavior at
all - `AuthorizationBehavior<TRequest, TResponse>` still declares exactly the same two type parameters it always
did, mapping 1:1 onto `IPipelineBehavior<TRequest, TResponse>` exactly as ADR-0023 requires. The case-only
generic parameter lives on a static factory method (`Eject.WithCase<TCase>`) and, type-erased, on an exception
(`PipelineEjectCaseException`) - neither is a DI-resolved component, so neither is subject to the open-generic
resolver's positional-substitution constraint ADR-0020 and ADR-0023 both already describe.

## Consequences

- **Positive:** `AuthorizationBehavior` needs no `IOutcomeCaseRegistry` dependency and no longer constructs its
  own response - a genuine simplification of that behavior's own code and dependency list, not just a paper win,
  confirmed by an implementation rather than assumed from a proposal.
- **Positive:** The remaining migration cost, if and when pursued, is measured as low and uniform across all
  five remaining behaviors - the spike found no behavior-specific complication, so this is "one pattern, applied
  five more times," not five open investigations.
- **Positive:** The DI registration surface needed no change - `InterlocutorPipelineBuilder`'s
  `AuthorizationBehaviorDescriptor` still lists `IOutcomeCaseRegistry` as a default dependency to try-add,
  `PipelineChainExecutor` resolves it lazily only when a case-eject actually occurs, and a consumer with no
  case-ejecting behavior registered never pays for or needs the registration.
- **Negative, accepted permanently: behavior-specific error-message context is lost.** Before migration,
  `AuthorizationBehavior` passed `ConstructOrThrow` its own name (`"AuthorizationBehavior<TRequest, TResponse>"`),
  an event phrase (`"denied a request"`), and a requirement clause naming `IRequiresPolicyRequest` and
  `Forbidden` by name - all of it lost once the behavior no longer participates in constructing the exception
  message, because the shared catch site cannot know which behavior, or which marker interface, is responsible
  for a given eject. The replacement message ("Eject.WithCase ejected a `{CaseType}` case, but `{TResponse}` has
  no `{CaseType}` case to construct...") is generic across every behavior that migrates to this mechanism rather
  than naming the specific one. This is a genuine, permanent trade-off of the design, not an implementation gap
  that can be patched without cost: restoring per-behavior phrasing would require
  `PipelineEjectCaseException` to grow optional context fields every migrated behavior populates, eroding the
  "no `TResponse` knowledge, no `IOutcomeCaseRegistry` dependency at all" ergonomic win this design exists to
  provide. A consumer whose tooling matches on the old message's specificity (rather than on `CaseType`, which
  the new message still names) will see a real, user-facing regression in diagnostic quality on the fail-fast
  path [ADR-0023](0023-pipeline-behavior-case-construction-fail-fast.md) commits to as a permanent guarantee -
  the guarantee that a mismatch fails loudly is preserved; the guarantee that the message identifies *which
  behavior* is not.
- **Negative, accepted, not yet done: five behaviors remain outstanding.** `CancellationBehavior`,
  `TimeoutBehavior`, `RateLimitedBehavior`, `ValidationBehavior`, and `FluentValidationErrorsBehavior` still take
  `IOutcomeCaseRegistry` and call `ConstructOrThrow` directly; no behavior change or bug, but a real
  inconsistency in mechanism across otherwise-identical behaviors. This remains open, explicitly-scoped
  follow-on work, not a silently deferred gap. The generator-emitter gap this bullet originally paired with it
  is closed (see "Current shipped scope" above) - migrating any of the remaining five behaviors is now equally
  safe on both dispatch paths, since `DispatchWrapperEmitter` no longer has a mechanism of its own to keep in
  sync.
- **Negative:** `OutcomeCaseRegistryExtensions.ConstructOrThrow<TResponse>(IOutcomeCaseRegistry, PipelineEjectCaseException)`
  adds a second, distinct reflection call per eject (`MethodInfo.MakeGenericMethod`, alongside the existing
  `UnionCaseFactory.FindCaseConstructor` lookup `Construct<TResponse, TCase>` already performs) and a
  corresponding IL2060 trim-analysis warning. Both are cached per-process (keyed by `(TResponse, CaseType)`), so
  steady-state cost is a dictionary lookup either way - this does not newly break trimming
  ([ADR-0016](0016-trim-compatible-not-aot-compatible.md)'s `Interlocutor.Abstractions` is already
  `IsTrimmable` but not `IsAotCompatible`, for the same underlying reflection reason `DefaultOutcomeCaseRegistry`
  isn't) - but it is one more moving part per case-eject, not a straightforward net reduction in reflection.

## Alternatives Considered

- **`PipelineEjectCaseException<TCase>`, generic over the case type, as the ticket's original working name
  suggested.** Rejected - does not compile against the shared catch site's actual generic shape. See "The
  shape" above: `PipelineChainExecutor.ExecuteAsync<TRequest, TResponse>` cannot also declare a compile-time
  `TCase` to catch, since every migrated behavior's ejected case type differs but all reach the identical two
  catch clauses.
- **`Eject.WithCase<TCase>` throwing internally, mirroring `Eject.With<TResponse>`'s shape exactly, so
  `return Eject.WithCase(new Forbidden(...));` would read the same as `Eject.With`.** Rejected - does not
  compile. `WithCase<TCase>`'s only generic parameter is the case type, not the response type, so a value typed
  to it is not assignment-compatible with the enclosing method's actual `Task<TResponse>` return type; the
  method must instead construct and return the exception itself, for the caller to throw.
- **Decomposing `TResponse` into a fixed failure-case generic parameter on the behavior**, as already rejected
  by [ADR-0020](0020-validation-failure-case-fidelity.md) and reaffirmed by
  [ADR-0023](0023-pipeline-behavior-case-construction-fail-fast.md). Not reopened here - see "Consistency with
  ADR-0023 and ADR-0020" above for why `Eject.WithCase<TCase>` is a categorically different shape (a case-only
  generic parameter on a static factory and an exception, not a third open generic parameter on a DI-resolved
  behavior) that does not run into the open-generic resolver constraint that alternative was rejected for.
- **Migrating all six behaviors and the generator emitter in the same change as the worked example**, rather
  than shipping `AuthorizationBehavior` alone and recording the rest as follow-on work. Rejected for this
  change: the spike's purpose was to measure the blast radius with one real worked example before committing to
  the full migration, and the generator-emitter half specifically was explicitly out of the spike's scope. Fully
  measuring and recording that scope (this ADR) before doing the remaining work keeps the record honest about
  what is actually shipped versus merely sized.

## Follow-on work

- Migrate `CancellationBehavior`, `TimeoutBehavior`, `RateLimitedBehavior`, `ValidationBehavior`, and
  `FluentValidationErrorsBehavior` to `Eject.WithCase`, each a mechanical repeat of the
  `AuthorizationBehavior` migration.
- ~~Update `Interlocutor.Generator`'s `DispatchWrapperEmitter` to emit a `PipelineEjectCaseException` catch
  block alongside its existing `PipelineEjectException<TResponse>` block at both emitted catch sites~~ - **done**,
  by a deeper fix than this bullet originally called for: `DispatchWrapperEmitter` now emits a single call into
  the shared `PipelineChainExecutor.ExecuteAsync` instead of re-deriving the fold/eject-catch logic as
  string-templated source at all, so a compile-time-discovery consumer gets the same graceful
  `InvalidOperationException` fail-fast behavior the hand-written-wrapper path already has, with no
  generator-side catch clause left to keep in sync by hand.
- This closed item was not required for `AuthorizationBehavior`'s current migration to be correct for a
  consumer on the reflection/`ScanAssembly` discovery path ([ADR-0008](0008-compile-time-handler-discovery.md)),
  since that path dispatches through the hand-written wrappers and `PipelineChainExecutor` already covered it;
  it was required before a compile-time-discovery consumer could safely register `AuthorizationBehavior` (or
  any future behavior migrated the same way) - that condition is now satisfied.
