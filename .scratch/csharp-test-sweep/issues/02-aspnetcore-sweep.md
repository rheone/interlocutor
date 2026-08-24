# 02 - Interlocutor.AspNetCore test sweep

**What to build:** (n/a — this documents completed test-quality work, not planned work)

**Blocked by:** None - can start immediately

**Status:** done, with one follow-up needing human judgment (see Follow-ups)

- [x] Sweep applied to tests/Interlocutor.AspNetCore.Tests
- [x] Build passes
- [x] Full test run passes

## Discovery findings

- Test framework: xUnit v3 (`xunit.v3`). Mocking library: NSubstitute.
- `TreatWarningsAsErrors` is off solution-wide (commented out in `Directory.Build.props`), so any ambiguity
  annotation would use `#warning` rather than `// SWEEP-AMBIGUITY:` — none were needed.
- No duplicate test display names (`dotnet test --list-tests | sort | uniq -d` empty).
- No `[Fact(Skip` / `[Theory(Skip` anywhere in the project.
- No `async void` test methods.
- No assertion anti-patterns (`Assert.True(x == y)`, `Assert.Equal(true/false/null, ...)`, etc.).
- No `[Theory]`/`TheoryData` usage in this project at all, so no serializer audit applied.
- No `internal` members in `src/Interlocutor.AspNetCore`, so no `InternalsVisibleTo` gap.
- Helper/fixture files correctly have no `[Fact]`/`[Theory]` and were skipped: `CustomOperationOutcome.cs`,
  `CustomRateLimited.cs`, `TeapotOutcomeStatusMapper.cs`.
- Missing test file found: `src/Interlocutor.AspNetCore/AspNetCoreRequestContextAccessor.cs` had no dedicated
  unit test class — only indirect coverage via `AuditingBehaviorAspNetCoreIntegrationTests.cs`, which exercises
  it end to end through `AuditingBehavior` rather than testing its `Context` getter/setter contract directly.
- Coverage gap found: the arity-4 `ToActionResult<TSuccess, TCase2, TCase3, TCase4>` and
  `ToMinimalApiResult<TSuccess, TCase2, TCase3, TCase4>` overloads on `ResultExtensions` had zero direct test
  coverage — arity 2, 3, 5, and 6 were all exercised, arity 4 was skipped entirely in both
  `ResultExtensionsTests.cs` and `MinimalApiResultExtensionsTests.cs`.
- No infrastructure (real DB/network/filesystem) is required by this project. `ResultExtensionsHttpHostTests.cs`
  uses ASP.NET Core's in-memory `TestServer`/`WebApplicationBuilder` — an in-process test host, not a real
  network listener — so it needs no special CI setup.
- No `[Obsolete]`-marked members exist in `src/Interlocutor.AspNetCore`, so the obsolete-member testing/pragma
  instruction had nothing to apply to.

## Files touched

- `tests/Interlocutor.AspNetCore.Tests/DefaultOutcomeStatusMapperTests.cs` — retrofitted Arrange/Act/Assert
  section comments onto every existing test (converted several expression-bodied one-liners to block bodies to
  make the three sections explicit). No coverage or behavioral gaps found in this class.
- `tests/Interlocutor.AspNetCore.Tests/GenericCatchAllResultExtensionsTests.cs` — retrofitted AAA comments onto
  every existing test. No coverage gaps found.
- `tests/Interlocutor.AspNetCore.Tests/MinimalApiResultExtensionsTests.cs` — retrofitted AAA comments onto every
  existing test; added 4 new tests filling the arity-4 `ToMinimalApiResult` gap (success, `NotFound`,
  `Forbidden`, `ValidationErrors` cases), tagged `// Auto Generated, verify expected behavior:`.
- `tests/Interlocutor.AspNetCore.Tests/ResultExtensionsHttpHostTests.cs` — retrofitted AAA comments onto every
  existing test (the shared `TestServer`/`HttpClient` setup lives in `InitializeAsync`, noted as such in each
  Arrange section).
- `tests/Interlocutor.AspNetCore.Tests/ResultExtensionsTests.cs` — retrofitted AAA comments onto every existing
  test; added a one-line class summary doc comment for consistency with sibling test classes in this project;
  added 4 new tests filling the arity-4 `ToActionResult` gap (success, `NotFound`, `Forbidden`,
  `ValidationErrors` cases), tagged `// Auto Generated, verify expected behavior:`.
- `tests/Interlocutor.AspNetCore.Tests/AspNetCoreRequestContextAccessorTests.cs` — **new file**, filling the
  missing-test-file gap for `AspNetCoreRequestContextAccessor`. 10 tests covering: null-constructor-argument
  guard, null `HttpContext` → null `Context`, correlation ID from header, user ID from authenticated identity,
  user ID left null when unauthenticated, tenant ID from header, and the `Context` setter's
  `NotSupportedException`. All tagged `// Auto Generated, verify expected behavior:` and all use AAA comments.
- `tests/Interlocutor.AspNetCore.Tests/AuditingBehaviorAspNetCoreIntegrationTests.cs` — reviewed, already had
  correct AAA comments; no changes needed.

## Coverage gaps filled

1. `AspNetCoreRequestContextAccessor` — new dedicated unit test file (`AspNetCoreRequestContextAccessorTests.cs`),
   10 tests.
2. `ResultExtensions.ToActionResult<TSuccess, TCase2, TCase3, TCase4>` (arity-4) — 4 new tests in
   `ResultExtensionsTests.cs`.
3. `ResultExtensions.ToMinimalApiResult<TSuccess, TCase2, TCase3, TCase4>` (arity-4) — 4 new tests in
   `MinimalApiResultExtensionsTests.cs`.

## Quality fixes applied

- Retrofitted `// Arrange` / `// Act` / `// Assert` (or `// Arrange & Act` / `// Act & Assert` where one
  statement legitimately covers both) section comments onto every existing test method across all five
  pre-existing real test classes, not just the newly added tests.
- Converted expression-bodied one-line test methods in `DefaultOutcomeStatusMapperTests.cs` to block bodies so
  the three AAA sections could be made explicit and separately commented.
- Added a missing one-line class summary XML doc comment to `ResultExtensionsTests` for consistency with every
  other test class in the project (all of which already had one).

## Real bug surfaced while writing new coverage (not fixed — out of scope for a test sweep)

Writing direct unit tests for `AspNetCoreRequestContextAccessor.Context` surfaced that its documented
header-then-fallback behavior for `CorrelationId` and `TenantId` never actually triggers the fallback:

```csharp
var correlationId = httpContext.Request.Headers["X-Correlation-ID"].ToString() ?? httpContext.TraceIdentifier;
var tenantId = httpContext.Request.Headers["X-Tenant-ID"].ToString() ?? httpContext.User?.FindFirst("tenant_id")?.Value;
```

`StringValues.ToString()` returns `""` for a missing header, never `null`, so the `??` fallback (to
`TraceIdentifier`, or to the `tenant_id` claim) is unreachable dead code — a request with no `X-Correlation-ID`
header gets `CorrelationId = ""` instead of the trace identifier the XML doc comment on the class describes, and
likewise `TenantId` ends up `""` instead of falling back to the claim. The three affected tests
(`Context_WithoutCorrelationIdHeader_DoesNotFallBackToTraceIdentifier`,
`Context_WithoutTenantIdHeader_DoesNotFallBackToTenantIdClaim`,
`Context_WithoutTenantIdHeaderOrClaim_LeavesTenantIdEmpty`) assert the actual current (empty-string) behavior
and are tagged `// Auto Generated, verify expected behavior:` with an inline explanation. Fixing the production
code (likely `httpContext.Request.Headers["X-Correlation-ID"] is { Count: > 0 } value ? value.ToString() :
httpContext.TraceIdentifier`, and the analogous fix for tenant ID) is left as a follow-up — a test sweep fills
and documents coverage, it doesn't change production behavior.

## SWEEP-AMBIGUITY / #warning annotations

None. `TreatWarningsAsErrors` is off, so any needed annotation would have used `#warning`, but no case in this
project needed one — the ambiguity found (the dead fallback logic above) was resolved by testing actual
behavior with an explanatory comment, not by leaving an open warning in production code.

## Anything flagged but not fixed

- The `AspNetCoreRequestContextAccessor` correlation-ID/tenant-ID fallback bug described above — flagged with
  test-level documentation and comments, not fixed, since a bug fix changes production behavior and belongs in
  its own ticket/PR, not folded into a test-quality sweep.

## Verification

- `dotnet build tests/Interlocutor.AspNetCore.Tests/Interlocutor.AspNetCore.Tests.csproj` — succeeds, 0 warnings,
  0 errors (verified after every class and again as the final step).
- Full test run (`Interlocutor.AspNetCore.Tests` executable, no filter) — **99 total, 99 succeeded, 0 failed, 0
  skipped**.

## Follow-ups

- Consider fixing the `AspNetCoreRequestContextAccessor.Context` correlation-ID/tenant-ID fallback bug described
  above in a separate ticket — either fix the code to match the documented fallback behavior, or update the XML
  doc comment to describe the actual (no-fallback) behavior, whichever is intended.
