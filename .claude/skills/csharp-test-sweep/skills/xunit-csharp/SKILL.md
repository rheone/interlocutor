---
name: xunit-csharp
description: Write, update, and improve xUnit v3 unit tests in C# projects. Use when writing or reviewing C# unit tests, adding test coverage, or following project test standards.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert Engelhardt <rheone@gmail.com>
  version: 2.0.0
---

# xUnit C# Testing Skill

Framework-specific rules for xUnit v3. Framework-agnostic rules (naming, AAA structure, one-scenario-per-test, no static state, etc.) are in the [General Quality Checklist](../../references/quality-checklist.md) — apply those first, then these.

> This skill is invoked by [`csharp-test-sweep`](../../SKILL.md).

## Scope Selection

| Scope | Apply to |
|---|---|
| **Single test** | One `[Fact]` or `[Theory]` method |
| **Group** | Related tests within one `#region` |
| **Class** | Entire test class file |
| **Namespace** | All classes in a namespace |
| **Project** | All classes in a project |

**Test type:** Default is **unit tests**. If standalone, ask: "Are these unit tests, or do some require real infrastructure? [Y = include integration tests / N = unit tests only]". If invoked via sweep, inherit its answer.

## Framework Checklist

Apply these xUnit-specific checks during every sweep, after the [General Quality Checklist](../../references/quality-checklist.md).

- [ ] `[Fact]` vs `[Theory]` correctly chosen — single-row `[Theory]` converted to `[Fact]`
- [ ] Strongly-typed `TheoryData<T>` used, not `IEnumerable<object[]>`
- [ ] `Assert.Equivalent` used for structural comparison when type lacks `Equals`
- [ ] `Assert.Multiple` used for batched independent assertions in one scenario
- [ ] Exception assertions verify a property on the exception (not just `.Throws`)
- [ ] `CancellationToken.None` used (never `default`)
- [ ] `ITestOutputHelper` used for diagnostic context only, not as assertion substitute
- [ ] Theory data selected by equivalence partitioning with documented partitions
- [ ] xUnit v3 API used (not v1/v2 patterns like `IXunitSerializable`)
- [ ] Skipped tests include a ticket reference in the `Skip` reason
- [ ] Integration tests tagged with `[Trait("Category", "Integration")]`

## Core Rules

### `[Fact]` vs `[Theory]`

- `[Fact]`: exactly **one** meaningful scenario; parameterizing adds no value.
- `[Theory]`: **two or more** distinct input/output combinations, each from a different equivalence partition or boundary.
- Single-row `[Theory]` → convert to `[Fact]` (see quality-checklist).

### Theory Data

- Prefer strongly-typed `TheoryData<T1, T2, ...>` with `[MemberData]`.
- `[InlineData]` is acceptable only for simple primitive literals.
- Never use `IEnumerable<object[]>`.
- Field naming: `{TestMethodName}_Test_Data`.
- See [references/theorydata.md](references/theorydata.md) for conversion patterns.

### Equivalence Partitioning

Select theory data by partitioning the input space into behavioral partitions. Include one representative per partition plus boundary values. Document which partition each row represents in the `<summary>`.

> Default: one case per partition. Add more only when branching within a partition requires it — state the reason in the `<summary>`.

### Exception Assertions

Use `Assert.Throws<T>` (sync) or `Assert.ThrowsAsync<T>` (async). Always verify a property on the returned exception to prevent false positives:

```csharp
var ex = Assert.Throws<ArgumentNullException>(() => Subnet.Parse(null));
Assert.Contains("input", ex.ParamName);
```

### Async Tests

- Return `Task`, never `async void`.
- If the method under test is async, the test must be async.
- `ConfigureAwait(false)` is not required in test methods.

### CancellationToken

Pass `CancellationToken.None` (never `default`) when cancellation is not the subject. For cancellation testing: cancel a `CancellationTokenSource` and assert `OperationCanceledException`. See [EXAMPLES.md](EXAMPLES.md) for a full example.

### Assert.Multiple

Use for a single scenario where multiple independent properties must be verified and you want all failures in one run:

```csharp
Assert.Multiple(
    () => Assert.Equal(expectedHead, result.Head),
    () => Assert.Equal(expectedTail, result.Tail)
);
```

Do not use `Assert.Multiple` to combine what should be separate test cases.

### Assert.Equivalent

Use for structural comparison when the type does not override `Equals`. Prefer `Assert.Equal` when the type implements value equality. Never override `Equals` on a production type solely to satisfy a test assertion.

### String Input Coverage

When a method accepts `string`, include: `null`, `""`, `" "`, `"\t"`/`"\n"`/`"\r"`, and at least one valid input.

### Null Parameter Testing

Every reference-type parameter in a public constructor or method must have a test passing `null` and asserting `ArgumentNullException`.

### Test Output (`ITestOutputHelper`)

- Diagnostic context only — logging generated inputs, timing data.
- Never a substitute for assertions.
- Inject via primary constructor: `public class Tests(ITestOutputHelper output)`.
- Most tests do not need it. Do not inject without a clear diagnostic need.

### Test Fixtures

| Situation | Use |
|---|---|
| Cheap, pure setup | Constructor / `IDisposable` |
| Async setup or teardown | `IAsyncLifetime` |
| Expensive setup shared per-class | `IClassFixture<T>` |
| Shared across classes | `ICollectionFixture<T>` + `[Collection]` |

Never call async methods synchronously (`.Result`, `.GetAwaiter().GetResult()`) — use `IAsyncLifetime`. See [references/Fixtures.md](references/Fixtures.md) for wiring.

### Object Mother

Named canonical instances for values reused across tests. Methods return new instances each call. Colocate for single-class use; place under `TestData/` for project-wide reuse. See [references/ObjectMother.md](references/ObjectMother.md).

### Coverage Gaps

When working at class or project scope, run the sweep's gap detection checklist before writing. Present gaps to the user and ask for confirmation before implementing.

### Private / Internal Members

1. Test through the public API first.
2. Use `[assembly: InternalsVisibleTo]` for `internal` members.
3. Reflection as last resort.

### Test Isolation

No `Thread.Sleep` — arrange deterministic conditions instead.

### Skipping Tests

`[Fact(Skip = "...")]` / `[Theory(Skip = "...")]` requires written justification (see quality-checklist). Prefer a ticket reference in the reason.

### xUnit v1/v2 → v3 Upgrade

| v1/v2 | v3 |
|---|---|
| `IXunitSerializable` on the type | `IXunitSerializer` + `[RegisterXunitSerializer]` |
| `[assembly: CollectionBehavior]` | Removed — parallelism on by default |
| `xunit.runner.visualstudio` | Use v3-compatible runner package |

### Integration Tests

Apply only when opted in during scope selection:

- Use `IClassFixture<T>` / `ICollectionFixture<T>` for shared resources.
- Each test leaves infrastructure clean. Use `IAsyncLifetime.DisposeAsync` for teardown.
- Do not mock infrastructure — verify the real interaction.
- Group sharing classes under `[Collection]`.
- Tag with `[Trait("Category", "Integration")]`.

## Related Skills

- [`csharp-test-sweep`](../../SKILL.md) — orchestrates project-wide test quality improvements.
- [General Quality Checklist](../../references/quality-checklist.md) — framework-agnostic rules applied at every class.
- [REFERENCE.md](REFERENCE.md) — API lookup tables and assertion anti-patterns.
- [EXAMPLES.md](EXAMPLES.md) — worked examples.
- [ANTI-PATTERNS.md](ANTI-PATTERNS.md) — framework-specific pitfalls.
