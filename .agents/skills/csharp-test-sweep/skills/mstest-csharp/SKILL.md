---
name: mstest-csharp
description: Write, update, and improve MSTest unit tests in C# projects. Covers naming conventions, AAA structure, parameterized tests, mocking, coverage gap detection, fixture patterns, and MSTest-specific patterns. Use when writing or reviewing C# unit tests in MSTest projects.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert Engelhardt <rheone@gmail.com>
  version: 2.0.0
---

# MSTest C# Testing Skill

Targets **MSTest v4** (MSTest 4.x). Classic assertion style — use `Assert.AreEqual(expected, actual)`, not constraint-based syntax.

## Framework Checklist

Apply these MSTest-specific checks during every sweep, after the [General Quality Checklist](../../references/quality-checklist.md).

- [ ] `[TestClass]` on all test classes
- [ ] `[TestMethod]` on all test methods (not `[DataTestMethod]` — unnecessary in v3+)
- [ ] `Assert.AreEqual(expected, actual)` with correct argument order (expected first)
- [ ] `Assert.ThrowsException<T>` / `Assert.ThrowsExceptionAsync<T>` for exception testing
- [ ] `Assert.Multiple` used for batched independent assertions in one scenario
- [ ] `[TestInitialize]` / `[TestCleanup]` used for per-test setup/teardown (not constructor)
- [ ] `[Ignore]` includes a comment with reason
- [ ] `[DiscoverInternals]` used instead of making internal members public

## Naming

- `{Member}_{Scenario}_{Expectation}_Test`
- Example: `Divide_ByZero_ThrowsDivideByZeroException_Test`

## Structure

Every test follows **Arrange / Act / Assert** with section comments:

```csharp
// Arrange
// Act
// Assert
```

## Required Attributes

- `[TestClass]` on every test class
- `[TestMethod]` on every test method (not `[DataTestMethod]` — `[TestMethod]` works with `[DataRow]` in v3+)

## Parameterized Tests

- `[DataRow(values...)]` for value-based parameters
- `[DynamicData(nameof(PropertyOrMethod), DynamicDataSourceType.Property)]` for complex/computed data sources
- Each parameterized test covers: happy path, null/empty/whitespace, boundary values

## Assertions

- `Assert.AreEqual(expected, actual)` — expected first, actual second
- `Assert.IsTrue(condition)` / `Assert.IsFalse(condition)` for booleans
- `Assert.ThrowsException<T>(() => ...)` for synchronous exceptions
- `await Assert.ThrowsExceptionAsync<T>(() => asyncCall())` for async exceptions
- `Assert.IsNull(obj)` / `Assert.IsNotNull(obj)`
- `Assert.IsInstanceOfType(obj, typeof(T))`
- `Assert.Fail(message)` to force failure
- `Assert.Inconclusive(message)` for incomplete tests (must link a tracking item)
- `Assert.Multiple(() => { ... })` for batched independent assertions

## Specialized Assertions

- `StringAssert.Contains(actual, substring)`
- `StringAssert.Matches(actual, regex)`
- `CollectionAssert.AreEqual(expected, actual)` — ordered comparison
- `CollectionAssert.AreEquivalent(expected, actual)` — unordered comparison
- `CollectionAssert.AllItemsAreUnique(collection)`
- `CollectionAssert.IsSubsetOf(subset, superset)`

## Async Tests

- Return `Task` or `ValueTask` — never `async void`
- `await` async methods under test
- `await Assert.ThrowsExceptionAsync<T>(async () => ...)`

## Test Lifecycle

| Scope | Attribute | Signature |
|-------|-----------|-----------|
| Per-test setup | `[TestInitialize]` | `public void Init()` |
| Per-test cleanup | `[TestCleanup]` | `public void Cleanup()` |
| Per-class setup | `[ClassInitialize]` | `public static void InitClass(TestContext ctx)` |
| Per-class cleanup | `[ClassCleanup]` | `public static void CleanupClass()` |
| Per-assembly setup | `[AssemblyInitialize]` | `public static void InitAssembly(TestContext ctx)` |
| Per-assembly cleanup | `[AssemblyCleanup]` | `public static void CleanupAssembly()` |

- ClassInitialize/Cleanup must be `static`
- AssemblyInitialize/Cleanup must be `static`

## Execution Control

- `[Ignore]` — add a `// reason` comment, do NOT pass a message string to the attribute
- `[Timeout(milliseconds)]` — fail test if it exceeds the limit
- `[Priority(N)]` — ordering hint for Visual Studio Test Explorer
- `[DoNotParallelize]` — prevent parallel execution
- `[STATestClass]` / `[STATestMethod]` — for tests requiring STA apartment
- `[Retry(count)]` — retry flaky tests (use sparingly)

## Metadata & Traceability

- `[TestCategory("CategoryName")]` — group tests into categories
- `[TestProperty("key", "value")]` — custom key/value metadata
- `[Owner("name")]` — test owner
- `[WorkItem(id)]` — link to Azure DevOps work item
- `[GitHubWorkItem(id)]` — link to GitHub issue
- `[Description("text")]` — descriptive text

## Discover Internals

Apply `[assembly: DiscoverInternals]` in the test project to let MSTest discover `internal` test methods without making them public.

## Mocking

Never mock the class under test. See the companion mocking skill for the detected library (e.g. [`nsubstitute-csharp`](../nsubstitute-csharp/SKILL.md), [`moq-csharp`](../moq-csharp/SKILL.md)).

## String Input Coverage

Cover these cases: `null`, `string.Empty`, whitespace-only, control characters, valid input.

## Static State

No shared static mutable state between tests. Each test must be independent.

## Related Skills

Invoked automatically by [`csharp-test-sweep`](../../SKILL.md) when it detects MSTest in the project file.
