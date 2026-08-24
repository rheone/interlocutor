# 03 - Interlocutor.CommonKernel test sweep

**What to build:** (n/a — this documents completed test-quality work, not planned work)

**Blocked by:** None - can start immediately

**Status:** done

- [x] Sweep applied to tests/Interlocutor.CommonKernel.Tests
- [x] Build passes
- [x] Full test run passes

## What was done

This ticket picks up where the parent-session pass left off (FailureTests.cs, CancelledTests.cs,
RateLimitedTests.cs, TimeoutTests.cs, ResultTypeToStringTests.cs were already reviewed and are
unchanged). This session finished the remaining seven files:

- `AggregateResultTypesTests.cs`
- `OperationOutcomeTests.cs`
- `OperationOutcomeOfTwoCasesTests.cs`
- `OperationOutcomeOfThreeCasesTests.cs`
- `OperationOutcomeOfFourCasesTests.cs`
- `OperationOutcomeOfFiveCasesTests.cs`
- `OperationOutcomeOfSixCasesTests.cs`

No other test files exist in the project (`tests/Interlocutor.CommonKernel.Tests`) beyond the
twelve listed across both sessions.

### Change applied to every file above

Retrofitted `// Arrange` / `// Act` / `// Assert` (or `// Arrange & Act` for tests that construct
and immediately expose the value under test in one statement) section comments onto every existing
`[Fact]` method — none had them before. For tests that previously called `Assert.Equal(expected,
someExpression.Member(...))` inline, the expression was hoisted into a locally-named variable in
the Act section (e.g. `var value = result.GetValue();`) so the Act/Assert split is real rather than
cosmetic.

### Coverage gaps

None filled. Discovery found no missing coverage in these files — every public accessor,
constructor guard (null/empty-array throw), `ToString()` override, and switch/pattern-match path
already has a corresponding test.

One design pattern to flag rather than fix: `OperationOutcomeOfFourCasesTests`,
`...OfFiveCasesTests`, and `...OfSixCasesTests` test `ImplicitConversion_...` and `GetValue_...`
only for the success case and the last (highest-numbered) failure case, not every intermediate
case — whereas `OperationOutcomeOfTwoCasesTests` and `...OfThreeCasesTests` exercise every case for
those two members. This is not a true coverage gap: `Construct_FromCaseN_...` and
`Switch_OverCaseN_MatchesCaseN` already exist for every case in every arity file, so the
constructor-overload-resolution and pattern-matching mechanisms are exhaustively covered per case;
`ImplicitConversion` and `GetValue` share the same underlying storage validated by those tests, and
sampling the first/last case is a reasonable tapering strategy as arity grows to avoid
combinatorial test bloat. Left as-is — no `// SWEEP-AMBIGUITY:` needed since this reads as
intentional, not an oversight.

### Quality violations

None found needing a fix. All tests already followed one-assertion-concern-per-test,
`Assert.Throws` (no try/catch), `Assert.Contains` for exception-message checks, no magic-value
repetition needing extraction (literal strings like `"hello"`, `"missing"` are used once or twice
per test and are not shared/repeated enough to warrant class-level `const`), no shared mutable
state, no `Thread.Sleep`/`Task.Delay`, no non-deterministic inputs, no mocking of the class under
test (this project has zero mocks — CommonKernel is dependency-free value types).

### Discovery audits completed this session

- **Duplicate test IDs**: `dotnet test --list-tests | sort | uniq -d` — no output, no duplicates.
- **Theory-data serializer registration**: no `TheoryData<T>` usage anywhere in the project (no
  `[Theory]` methods exist at all — every test in this project is a `[Fact]`). Nothing to
  register.
- **Skipped tests**: `grep -rn "\[Fact(Skip\|\[Theory(Skip"` — no matches.
- **`async void`**: `grep -rn "async void"` — no matches.
- **Test-project hygiene**: `Interlocutor.CommonKernel.Tests.csproj` has no `<IsPackable>` setting,
  but this matches every other test project in the repo (`grep -l IsPackable tests/*/*.csproj`
  returns nothing repo-wide) — a repo-wide convention, not a per-project defect, so left alone.
  Package references (xunit.v3, NSubstitute, coverlet.collector, Microsoft.Testing.Platform) are
  current, no outdated v1/v2 framework references.
- **Duplicate-test-pattern candidates for parameterization**: the `OperationOutcomeOf*CasesTests`
  files are structurally repetitive across files (by design — one file per arity, mirroring the
  five/six-case source types) but within each file no two `[Fact]` methods differ only by a literal
  value in a way that would collapse into a single `[Theory]` — each covers a distinct case/member
  combination with a different assertion shape. No refactor candidates found.
- **`[Obsolete]` members**: none exist in `Interlocutor.CommonKernel`, confirmed again this
  session — nothing to wrap in `#pragma warning disable CS0618`.

### Verification

- `dotnet build tests/Interlocutor.CommonKernel.Tests/Interlocutor.CommonKernel.Tests.csproj` —
  succeeded, 0 warnings, 0 errors.
- `dotnet test tests/Interlocutor.CommonKernel.Tests/Interlocutor.CommonKernel.Tests.csproj` fails
  in this environment with a pre-existing, unrelated CLI/SDK error: `Testing with VSTest target is
  no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later`. This is an
  environment quirk (the preview .NET 11 SDK's `dotnet test` bridge vs. the
  Microsoft.Testing.Platform-based xunit.v3 test host), not something introduced by this sweep.
  Worked around by running the built Microsoft.Testing.Platform executable directly:
  `tests/Interlocutor.CommonKernel.Tests/bin/Debug/net11.0/Interlocutor.CommonKernel.Tests` — all
  166 tests pass, 0 failed, 0 skipped.

## Follow-ups

- The `dotnet test` CLI invocation documented in `CLAUDE.md` currently fails in this environment
  with a VSTest-bridge error for this project (Microsoft.Testing.Platform / xunit.v3-based). Tests
  do pass when the built executable is run directly. This is an environment/SDK configuration
  concern outside the scope of a test-content sweep — worth a separate look at whether the SDK,
  `global.json`, or a `TestingPlatformDotnetTestSupport` MSBuild property needs updating so
  `dotnet test` works as documented.
