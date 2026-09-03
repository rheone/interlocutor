# 02 - Per-case-type-named TryGet accessors for concrete Outcome case types

**What to build:** every concrete case type in a `[GenerateOutcomeMembers]` union should get a `TryGet{TypeName}(out {Type} value)` accessor, in addition to the existing anonymous `TryGetValue(out {Type} value)` overload - e.g. `outcome.TryGetNotFound(out var nf)`. This makes a union's API self-documenting by case type name rather than by position, independent of whether the union has any success/failure meaning at all.

A case type that is one of the union's own open generic type parameters (as in the shipped `Outcome<TSuccessResult, TCase2, ...>` family, where every case type at the declaration site *is* a type parameter, not a concrete type) must be skipped - a type parameter's name carries no case-specific meaning, so `TryGetTSuccessResult(out TSuccessResult value)` would be a meaningless echo of the parameter name. Only concrete case types get named accessors.

**Blocked by:** None - can start immediately

**Status:** done

- [x] A custom union with concrete case types (e.g. the existing `PaymentOutcome` shape) exposes `TryGet{TypeName}(out {Type} value)` for each case, returning `true` and populating `value` only when that case is active, `false` otherwise.
- [x] The existing anonymous `TryGetValue(out {Type} value)` overload continues to be generated alongside the new named accessor for every case (additive, not a replacement).
- [x] For the shipped `Outcome<TSuccessResult, TCase2, ...>` family, where every case type is the union's own type parameter, no named accessors are generated - only the generic `TryGetValue<T>` overloads remain available for case access.
- [x] A custom generic union mixing one of its own type parameters with concrete case types (e.g. `union MyResult<TSuccess>(TSuccess, NotFound, Conflict)`) gets named accessors only for the concrete cases (`TryGetNotFound`, `TryGetConflict`), not for the type-parameter case.
- [x] `documentation/extensibility.md`'s custom-arity example is updated to show the new named accessor alongside the existing `Match`/`TryGetValue` usage.

## Comments

Added `OutcomeUnionModel.CaseAccessorNames` (parallel to `CaseTypeDisplayStrings`): the resolved case type's simple name (`ITypeSymbol.Name` - generic arguments never appear, so a closed `ValidationError<string>` case still names its accessor `TryGetValidationError`) for a concrete case, `null` when the case type is an `ITypeParameterSymbol` belonging to the union itself. `OutcomeMemberEmitter` extracted the shared `TryGetValue`/named-accessor body into `EmitTryGetAccessorBody` and calls it twice per concrete case (once for the anonymous overload, once more for the named one when `CaseAccessorNames[i]` is non-null) - identical body, different method name. This is the accessor-name computation ticket 03's `INTERLOC012` collision diagnostic is blocked on.

Five new tests in `OutcomeMemberGeneratorTests.cs`: concrete-only cases get every named accessor plus the anonymous overload still emitted for each; a mixed generic-plus-concrete union names only the concrete cases; the shipped-family shape (every case is the union's own type parameter) emits zero named accessors; and a runtime test proving a named accessor returns `true`/populates only when its own case is active, `false` otherwise when a different case is active. Full `Interlocutor.Generator.Tests` (102/102) and `Interlocutor.CommonKernel.Tests` (265/265, including `PaymentOutcome`) pass.
