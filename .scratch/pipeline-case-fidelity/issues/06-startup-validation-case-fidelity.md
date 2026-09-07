# 06 - Extend InterlocutorStartupValidation with an opt-in case-fidelity check

**What to build:** `InterlocutorStartupValidation.ValidateHandlerRegistrations` today only checks that every scanned request type has a registered handler - it says nothing about whether a request's paired response type can actually express the case a behavior attached to it needs. Add an opt-in extension (naming TBD, e.g. `ValidatePipelineCaseFidelity`) that, given a built `IServiceProvider`, checks: for each request type implementing `IRequiresPolicy`/`ICancellable`/`IRateLimited`/`IHasTimeout`, whether its paired response has a constructible matching case (mirroring issue 03's analyzer, using the same `UnionCaseFactory`/`IOutcomeCaseRegistry` machinery reflectively instead of at compile time); and, unlike the analyzer, for each request type with at least one `IValidator<TRequest>` actually registered in the provided `IServiceProvider`, whether its paired response has a constructible `ValidationErrors`/`FluentValidationErrors` case - a check only possible at this layer, since applicability there is a DI-registration fact, not a static marker interface. This is the primary safety net for consumers on the reflection/`ScanAssembly` discovery path (e.g. `TutorialProject`) who have no access to issue 03's compile-time diagnostic.

**Blocked by:** 02 (cites ADR-0022's documented scope split between compile-time and runtime coverage)

**Status:** done - implemented in `b9b9c05`. Cites ADR-0023, not ADR-0022 as this ticket's text names (renumbered - see ticket 02's status note).

- [ ] New opt-in validation method added alongside `ValidateHandlerRegistrations`, following its existing signature/chaining style (`this IServiceProvider`, returns the same provider)
- [ ] Covers the four marker-interface behaviors (mirroring issue 03's analyzer logic)
- [ ] Additionally covers `ValidationBehavior`/`FluentValidationErrorsBehavior` by checking actual `IValidator<TRequest>` registration in the built container - the one case issue 03's analyzer cannot reach
- [ ] Throws a single `InvalidOperationException` listing every mismatched request type found, matching `ValidateHandlerRegistrations`' existing "detailed list, not first-failure" style
- [ ] Documented as opt-in (not called automatically) with the same `[RequiresUnreferencedCode]`/`[RequiresDynamicCode]` treatment `ValidateHandlerRegistrations` already carries
