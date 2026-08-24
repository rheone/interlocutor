# 08 - Interlocutor.FluentValidation test sweep

**What to build:** (n/a — this documents completed test-quality work, not planned work)

**Blocked by:** None - can start immediately

**Status:** done

## Scope

- src project: `src/Interlocutor.FluentValidation` (one public type: `ValidationBehavior<TRequest, TResponse>`)
- test project: `tests/Interlocutor.FluentValidation.Tests` (one test class: `ValidationBehaviorTests`, plus
  helper types `TestRequest`, `TestRequestNameValidator`, `TestRequestAgeValidator`)

## Discovery findings

- **Duplicate test IDs**: none (`--list-tests` showed 7 distinct names, 0 duplicates).
- **Skipped tests**: none (`[Fact(Skip` / `[Theory(Skip` grep empty).
- **Assertion anti-patterns**: none of the checked patterns (`Assert.True(x == ...)`, `Assert.Equal(true/false/null, ...)`, etc.) present.
- **`async void` tests**: none.
- **Theory serializer audit**: n/a — no `[Theory]`/`TheoryData` usage in this project.
- **InternalsVisibleTo**: not needed — `ValidationBehavior` under test is public; the internal test-helper
  types (`TestRequest`, its validators) are only referenced from within the same test assembly.
- **Missing test files**: none — the project's only public type (`ValidationBehavior`) already has a
  matching, exercising test class.
- **Project hygiene**: test `.csproj` already has `xunit.v3`, `xunit.runner.visualstudio`,
  `Microsoft.Testing.Platform`, `Microsoft.NET.Test.Sdk`, `coverlet.collector`, `NSubstitute` +
  `NSubstitute.Analyzers.CSharp` — no outdated framework references found. (No explicit `IsPackable`
  check applies — test projects in this repo don't set it individually.)
- **Duplicate test patterns**: none warranting parameterization — the 7 existing tests each cover a
  distinct scenario with materially different request/validator/registry setups, not literal-only
  variance.
- **`[Obsolete]` members**: none exist in `ValidationBehavior` or its dependencies — nothing to wrap in
  `#pragma warning disable/restore CS0618`.
- **Real infrastructure**: none needed. `ValidationBehavior` only depends on `FluentValidation.IValidator<T>`
  and `IOutcomeCaseRegistry`, both fully exercisable with real (`AbstractValidator`-derived) or
  NSubstitute-mocked instances — no DB/network/filesystem required.

## What changed

File touched: `tests/Interlocutor.FluentValidation.Tests/ValidationBehaviorTests.cs`

1. **Retrofitted `// Arrange` / `// Act` / `// Assert` comments** onto all 7 pre-existing test methods (none had them).
2. **Renamed all test methods** to the `Handle_{Scenario}` convention used by the sibling
   `AuthorizationBehaviorTests`/`CancellationBehaviorTests` in `tests/Interlocutor.Abstractions.Tests` (this
   file previously omitted the member-under-test prefix entirely):
   - `NoValidators_PassesThrough` → `Handle_PassesThroughWhenNoValidatorsRegistered`
   - `PassingValidation_InvokesHandler` → `Handle_InvokesHandlerWhenValidationPasses`
   - `FailingValidation_ShortCircuitsHandler` → `Handle_DoesNotCallNextWhenValidationFails`
   - `FailingValidation_ReturnsAllErrorMessages` → `Handle_ReturnsAllErrorMessagesAcrossValidatorsWhenValidationFails`
   - `DuplicateErrors_DeDuplicated` → `Handle_DeDuplicatesErrorMessagesAcrossValidators`
   - `FailingValidation_WithResponseTypeThatHasNoValidationErrorsCase_ThrowsNamedException` →
     `Handle_WithResponseTypeThatHasNoValidationErrorsCase_ThrowsNamedExceptionOnFailure`
   - `FailingValidation_WithHandRolledOutcomeAndCustomRegistry_ConstructsValidationErrorsCase` →
     `Handle_HandRolledNonUnionResponse_ShortCircuitsWithValidationErrorsViaCustomRegistry`
3. **Filled two coverage gaps** (tagged `// Auto Generated, verify expected behavior:`):
   - `Handle_PreservesOrderOfFirstOccurrenceAcrossValidators` — asserts the aggregated error-message list
     preserves order of first occurrence across validators, a documented behavior (see the XML remarks
     in `ValidationBehavior.cs`) that no existing test actually asserted ordering for.
   - `Handle_ForwardsCancellationTokenToEachValidator` — asserts the `CancellationToken` passed into
     `Handle` is forwarded unchanged to `IValidator<T>.ValidateAsync`, using an NSubstitute mock of the
     validator dependency (not the class under test, which is allowed/expected per the sweep's mocking
     rule). Needed a public stand-in request type (`PublicTestRequest`, added at the bottom of the file)
     because Castle DynamicProxy (NSubstitute's proxy engine) cannot proxy `IValidator<T>` when `T` is an
     internal type — the existing internal `TestRequest` type couldn't be used for this specific test.

## Verification

- `dotnet build tests/Interlocutor.FluentValidation.Tests/Interlocutor.FluentValidation.Tests.csproj` — 0
  warnings, 0 errors (an interim SA1201 warning from initial placement of `PublicTestRequest` was fixed by
  moving the record to the end of the file, after all test methods).
- `dotnet build src/Interlocutor.FluentValidation/Interlocutor.FluentValidation.csproj` — 0 warnings, 0
  errors.
- Full test run (`Interlocutor.FluentValidation.Tests` binary, net11.0): **9 total, 9 succeeded, 0 failed,
  0 skipped** (was 7 before the sweep; the two coverage-gap tests bring it to 9).

## Ambiguities / annotations left in code

None. No `// SWEEP-AMBIGUITY:` or `#warning` markers were needed — the code and existing tests were
unambiguous throughout.

## Follow-ups

None outstanding — the project's small surface area (one behavior class) meant coverage and quality were
already close to complete; only the two gaps above and the naming/AAA-comment retrofit were needed.
