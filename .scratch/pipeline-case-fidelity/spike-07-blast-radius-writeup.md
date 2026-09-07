# Spike 07 blast-radius write-up: `Eject.WithCase<TCase>`, a type-erased Eject variant

**Branch:** `spike/eject-withcase-case-fidelity` (not merged - human review only, per the ticket's acceptance
criteria and the PRD's "commit or drop" framing for issue 08).

This is a spike write-up, not a design decision - issue 08 (excluded from this run) owns the actual
commit-or-drop call and any ADR. What follows is what was actually found by building the prototype, not a
proposal.

## What was built

- `Eject.WithCase<TCase>(TCase caseValue)` in `src/Interlocutor.Abstractions/Pipeline/Eject.cs` - generic only
  in the case type. Unlike `Eject.With<TResponse>` (which throws internally so a call reads naturally in a
  `return` statement), `WithCase` does **not** throw - it constructs and returns a
  `PipelineEjectCaseException`, so the call site writes `throw Eject.WithCase(new Forbidden(...));`, same as
  throwing any other exception. This is a real, if small, deviation from the ticket's literal phrasing
  ("throw `Eject.WithCase(...)` directly") turning out to require this shape rather than the
  `With`-style internal-throw shape - see "Surprises" below.
- `PipelineEjectCaseException` (non-generic - not `PipelineEjectCaseException<TCase>` as the ticket's working
  name suggested; see "Surprises") in
  `src/Interlocutor.Abstractions/Pipeline/PipelineEjectCaseException.cs`, carrying a boxed `object CaseValue`
  and its runtime `Type CaseType` - mirroring `IUnion.Value`'s own shape, per the ticket.
- `AuthorizationBehavior<TRequest, TResponse>` migrated: constructor no longer takes `IOutcomeCaseRegistry`;
  `HandleAsync` throws `Eject.WithCase(new Forbidden(...))` on denial instead of calling
  `caseRegistry.ConstructOrThrow<TResponse, Forbidden>(...)`.
- `PipelineChainExecutor.ExecuteAsync` (`src/Interlocutor.Abstractions/Pipeline/PipelineChainExecutor.cs`) -
  **not** `QueryHandlerWrapper` itself - gained a second `catch (PipelineEjectCaseException eject)` clause
  alongside its existing `PipelineEjectException<TResponse>` guard, at both layers it already guards (the
  innermost handler call and each wrapped behavior). It resolves `IOutcomeCaseRegistry` from the ambient
  `IServiceProvider` and calls a new bridging extension,
  `OutcomeCaseRegistryExtensions.ConstructOrThrow<TResponse>(IOutcomeCaseRegistry, PipelineEjectCaseException)`
  (`src/Interlocutor.Abstractions/Pipeline/Cases/OutcomeCaseRegistryExtensions.cs`), which closes
  `IOutcomeCaseRegistry.Construct<TResponse, TCase>` reflectively over the exception's runtime `CaseType` via
  `MethodInfo.MakeGenericMethod`, cached per `(TResponse, CaseType)` pair, and rewraps
  `OutcomeCaseNotSupportedException` into the same kind of named `InvalidOperationException` behaviors used to
  throw themselves.
- Tests: existing `AuthorizationBehaviorTests` adjusted (no more `IOutcomeCaseRegistry` constructor arg; each
  test that previously asserted on `behavior.HandleAsync`'s direct return now routes through a small
  `RunBehaviorAsync` helper that catches `PipelineEjectCaseException` and calls the same production
  `ConstructOrThrow<TResponse>` extension the real catch site calls - not a reimplementation, so a passing test
  is genuine evidence the production catch site produces the same result). Two similarly-shaped call sites in
  `IUnionOnlyResponseDispatchTests` and `ReorderedPipelineCompositionTests` needed the same treatment. A new
  `AuthorizationBehaviorEjectCaseEndToEndTests` file proves the full path through a real `Mediator.SendAsync`
  call (real DI container, real `QueryHandlerWrapper`/`PipelineChainExecutor`), for both the success path and
  the no-matching-case fail-fast path. All pre-existing tests pass with unchanged observed behavior; the whole
  solution (1292 tests) passes.

## Blast radius: all six behaviors, sized from what was actually learned

All six behaviors that short-circuit via `ConstructOrThrow` follow the **exact same shape**:
`IOutcomeCaseRegistry caseRegistry` in the constructor, a single `caseRegistry.ConstructOrThrow<TResponse,
TCase>(value, behaviorName, eventPhrase, requirementClause)` call at the one point they short-circuit. Having
now done this once, migrating the other five is mechanical, not exploratory - each is:

1. Drop `IOutcomeCaseRegistry` from the constructor.
2. Replace the `return caseRegistry.ConstructOrThrow<TResponse, TCase>(value, ...)` call with
   `throw Eject.WithCase(value)`.
3. Delete the now-dead private `Construct*Response` helper method, if the behavior had one (three of the six
   do: `AuthorizationBehavior`, `CancellationBehavior`, `RateLimitedBehavior`, `TimeoutBehavior` all had a
   `private TResponse Construct...Response(...)` wrapper around the call; `ValidationBehavior` and
   `FluentValidationErrorsBehavior` called it inline).

| Behavior | Case type | Constructor today | Change |
|---|---|---|---|
| `AuthorizationBehavior` | `Forbidden` | `(IPolicyEvaluator, IOutcomeCaseRegistry)` | **Done** in this spike |
| `CancellationBehavior` | `Cancelled` | `(IOutcomeCaseRegistry)` | Same shape, same size |
| `RateLimitedBehavior` | `RateLimited` | `(IRateLimiter, IOutcomeCaseRegistry)` | Same shape, same size |
| `TimeoutBehavior` | a timeout case | `(ITimeoutPolicy, IOutcomeCaseRegistry)` | Same shape, same size |
| `ValidationBehavior` (`Interlocutor.FluentValidation`) | `ValidationErrors` | `(IEnumerable<IValidator<TRequest>>, IOutcomeCaseRegistry)` | Same shape; lives in a different package (`Interlocutor.FluentValidation`), so this migration would be the first case-eject call site outside `Interlocutor.Abstractions` - no new dependency needed, since it already depends on Abstractions for `Eject`/`PipelineEjectCaseException` |
| `FluentValidationErrorsBehavior` (`Interlocutor.FluentValidation`) | `FluentValidationErrors` | `(IEnumerable<IValidator<TRequest>>, IOutcomeCaseRegistry)` | Same as above |

So: **all six** would need to change if adopted fully, and all six changes are the same shape as the one
worked example - there is no behavior among the six whose migration looks materially different, no matter what
its `TCase` is. This is a meaningfully smaller/flatter blast radius than "six independent investigations" - it
is "one pattern, applied six times."

### What changes in each wrapper's catch logic

This is where the actual finding differs from what the ticket's framing assumed. The ticket names
`QueryHandlerWrapper<TQuery, TResponse>`, `CommandHandlerWrapper<TCommand, TResponse>`, and
`VoidCommandHandlerWrapper<TCommand>` (now `BareCommandHandlerWrapper<TCommand, TResult>`) as three separate
places needing "a single `caseRegistry.Construct<TResponse, TCase>(caseValue)` call per case type instead of
each behavior doing its own." In the shipped code, **none of the three hand-written wrappers has its own catch
logic at all** - `QueryHandlerWrapper`, `CommandHandlerWrapper`, and `BareCommandHandlerWrapper` all resolve
their handler, tag the activity, and delegate entirely to one shared internal method,
`PipelineChainExecutor.ExecuteAsync<TRequest, TResponse>`, which is where every `PipelineEjectException<TResponse>`
guard already lived (a prior refactor already extracted this - see that file's own remarks). So the *actual*
blast radius on the hand-written wrapper side is: **one file, two catch clauses, already done in this spike** -
adding the four remaining behaviors changes nothing further here, because the catch site doesn't vary by
behavior or by wrapper kind, only by `TResponse`, which `PipelineChainExecutor` is already generic over.

The **generator's emitted dispatch wrappers are a different story and do need a parallel change** if this were
adopted fully. `src/Interlocutor.Generator/Emission/DispatchWrapperEmitter.cs` does not call
`PipelineChainExecutor` - it emits its own inlined string-templated equivalent (mirroring the same
try/catch-per-layer shape, per its own comment at line 122, "for exactly the same reason hand-written wrappers
would duplicate it, if the prefactor into `PipelineChainExecutor` hadn't happened for them"). Concretely, each
of the two emitted catch blocks (`HandleAsync(request, ct)` for the innermost handler call, and
`behavior.HandleAsync(request, next, ct)` for each wrapped behavior) would need a second generated
`catch (global::Interlocutor.Abstractions.Pipeline.PipelineEjectCaseException eject) { ... }` block emitted
alongside the existing `PipelineEjectException<TResponse>` one, resolving `IOutcomeCaseRegistry` from
`services` and calling the same `ConstructOrThrow<TResponse>` bridge extension. This is a small, mechanical
addition to the emitter (a few more `builder.AppendLine` calls per catch site, following the existing pattern
line-for-line) - **not implemented in this spike**, since the ticket scoped implementation to the hand-written
wrapper side, but it is real, necessary work if this is committed: a consumer using compile-time discovery
(`Interlocutor.Generator`) who registers `AuthorizationBehavior` (or any other migrated behavior) would get no
construction at all - the `PipelineEjectCaseException` would propagate unhandled up through
`IGeneratedRequestDispatcher.DispatchAsync` and out to the mediator's caller, violating the "never observed by
a caller of `IMediator`" contract - until the emitter is updated to match. This is the one place where "commit"
carries real, not-yet-sized follow-on work; everything else in this list was fully exercised in the prototype.

### Surprises

- **The exception ended up non-generic, not `PipelineEjectCaseException<TCase>` as the ticket's working name
  suggested.** A shared catch site (`PipelineChainExecutor.ExecuteAsync<TRequest, TResponse>`) is generic only
  over `TRequest`/`TResponse` - it cannot also declare a compile-time `TCase` to catch, since different
  behaviors eject different case types through the very same catch clause (`Forbidden` from
  `AuthorizationBehavior`, `Cancelled` from `CancellationBehavior`, etc., all reaching the identical two catch
  blocks). A single non-generic exception, carrying the case boxed as `object` plus its runtime `Type`, is what
  a single shared catch clause can actually catch regardless of which case type was ejected. The ticket's own
  phrasing ("or equivalent name") anticipated this might not survive contact with the type system, and it
  didn't.
- **`throw Eject.WithCase(new Forbidden(...))` does not compile as literally written, for the same
  type-erasure reason.** `Eject.With<TResponse>` works in a `throw`/`return` position because its return type
  is `TResponse` itself (it throws internally, but is typed to fit the calling method's own return type).
  `WithCase<TCase>` cannot do the same trick: its only generic parameter is `TCase` (e.g. `Forbidden`), which
  is not assignment-compatible with the behavior's actual `Task<TResponse>` return type, and `throw` requires
  its operand to already be an `Exception`. The fix that actually compiles: `WithCase<TCase>` does not throw
  internally at all - it **constructs and returns** the `PipelineEjectCaseException`, so the call site provides
  its own `throw` keyword, exactly like throwing any other exception (`throw Eject.WithCase(new
  Forbidden(...));`). This is a one-line difference from the ticket's literal example but a real design
  decision, not a typo - worth flagging since it means `Eject.With` and `Eject.WithCase` are used with
  superficially the same syntax but different mechanics (one throws internally and returns a value type-fitted
  to the enclosing return; the other returns an exception object for the caller to throw).
- **Losing behavior-specific error-message context is real, but turned out to not matter for the one test that
  checks it.** Before migration, `AuthorizationBehavior` passed `ConstructOrThrow` a behavior name
  (`"AuthorizationBehavior<TRequest, TResponse>"`), an event phrase (`"denied a request"`), and a requirement
  clause (`"A request implementing IRequiresPolicyRequest must use a response type with a Forbidden case"`) -
  all lost once the behavior no longer participates in constructing the exception message, since the shared
  catch site has no way to know which behavior, or which marker interface, is actually responsible for a given
  eject. The replacement message
  (`"Eject.WithCase ejected a {CaseType} case, but {TResponse} has no {CaseType} case to construct..."`) is
  generic across all six behaviors rather than naming the specific one. The existing test
  (`Handle_WithResponseTypeThatHasNoForbiddenCase_ThrowsNamedExceptionOnDenial`) only asserts
  `Assert.Contains("Forbidden", ex.Message)`, so this happened to still pass - but a stricter test, or a
  consumer relying on the old message's specificity for logging/alerting, would see a real behavior change.
  This is a genuine trade-off of the design, not an implementation gap that could be patched without cost: the
  entire point of the shared catch site is that it does not know which behavior ejected, so it cannot
  reconstruct behavior-specific phrasing without `PipelineEjectCaseException` growing optional context fields
  that every behavior would then need to populate - eroding some of the ergonomic win ("no knowledge of
  `TResponse` and no `IOutcomeCaseRegistry` dependency at all") the PRD is chasing.
- **`MethodInfo.MakeGenericMethod` triggers an IL2060 trim-analysis warning** on the new bridge extension
  (`Interlocutor.Abstractions` is `IsTrimmable` but deliberately not `IsAotCompatible`, already for reflection
  reasons - see that project's own `.csproj` comment). This doesn't newly break anything (the project already
  isn't AOT-compatible for the same underlying reason `DefaultOutcomeCaseRegistry.Construct` isn't), but it is
  a second, distinct reflection call per eject now: the existing `UnionCaseFactory.FindCaseConstructor` lookup
  inside `Construct<TResponse, TCase>` still runs (unavoidable - that's how a compiler-generated union's case
  constructor is found), *plus* the new `MakeGenericMethod` call needed to close that generic method over a
  `TCase` known only at run time. Both are cached (per-process, keyed by the relevant type pair), so the
  steady-state cost is a dictionary lookup either way, not repeated reflection - but it is one more moving
  part, not a straightforward net reduction in reflection.
- **The DI registration side needed no changes**, which was not obvious going in.
  `InterlocutorPipelineBuilder`'s `AuthorizationBehaviorDescriptor` still lists `IOutcomeCaseRegistry` as a
  default dependency to try-add alongside `AuthorizationBehavior` - left untouched in this spike, since
  `PipelineChainExecutor` still needs `IOutcomeCaseRegistry` resolvable from the container whenever *any*
  case-ejecting behavior (migrated or not) is registered, and it's resolved lazily (only inside the new catch
  clause, only when a case-eject actually happens) so a consumer with no case-ejecting behavior at all never
  pays for or needs the registration. No breaking change, no new registration surface.
- **The existing tests could not just be "unchanged except constructor arg"** - the ticket anticipated this
  ("adjust the tests' internals as needed"), and it was necessary in every affected test, not just some: every
  test that previously asserted on `behavior.HandleAsync(...)`'s direct return value now needs to either catch
  `PipelineEjectCaseException` and construct the response itself (simulating the wrapper), or accept that the
  behavior throws where it used to return. There is no way to keep a unit test of the behavior in isolation
  (no pipeline wrapper around it) passing without either reintroducing some construction logic into the test,
  or testing through the real `Mediator`/DI container instead (which this spike also added, as
  `AuthorizationBehaviorEjectCaseEndToEndTests`, precisely to have at least one test that doesn't rely on a
  hand-rolled stand-in for the wrapper's job).

## Recommendation (not a decision)

**Lean toward commit, conditional on issue 08 costing out the generator-side follow-on work and the
message-specificity trade-off explicitly**, rather than treating this write-up's clean build/test run as the
whole story. Reasoning:

- The mechanical migration cost for the remaining five behaviors is genuinely low and uniform - this spike
  found no behavior-specific wrinkle among the six, which was the main open question the "spike" framing
  existed to answer.
- The hand-written-wrapper side of the ticket's stated scope turned out to already be centralized
  (`PipelineChainExecutor`), so the real remaining cost of "full adoption" is concentrated almost entirely in
  one place this spike did not touch: `DispatchWrapperEmitter`. That is a bounded, well-understood piece of
  work (mirror an existing emitted catch block, once), not an open-ended one - but it is real work, not zero,
  and any commit decision should account for it rather than assume the hand-written-side experience
  generalizes for free.
- The efficiency case in the PRD ("shrinks the generic-exception-type surface... removes
  `IOutcomeCaseRegistry` as a per-behavior dependency") holds up under actual implementation - it is a genuine
  simplification of each behavior's own code and dependencies, not just a paper win.
- Weighing against a clean "commit": the lost behavior-specific error-message context is a real, user-facing
  regression in diagnostic quality for the fail-fast path ADR-0023 already commits to as a permanent guarantee
  - "fails fast" is preserved, but "identifying the behavior, what it was trying to do" (ADR-0023's own
  language for what the exception must name) is weakened unless a follow-up design restores it, which would
  add back some of the coupling this spike removes. Issue 08 should treat this as a named cost to accept or
  design around, not an oversight to silently fix.
