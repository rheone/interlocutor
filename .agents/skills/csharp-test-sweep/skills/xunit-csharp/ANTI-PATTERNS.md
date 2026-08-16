---
name: xunit-csharp-anti-patterns
description: Framework-specific pitfalls to avoid in xUnit v3 C# tests
license: Apache-2.0
metadata:
  author: Robert Engelhardt <rheone@gmail.com>
  version: 1.1.0
---

# xUnit v3 Anti-Patterns

> Framework-agnostic anti-patterns (no assertions, testing the class under test via mock, etc.) are in the [General Quality Checklist](../../references/quality-checklist.md). This file covers xUnit-specific pitfalls only.

---

### 1. `[Theory]` with Exactly One Data Row

A `[Theory]` with a single `[InlineData]` or one-element `MemberData` adds ceremony for no benefit. It also masks the intent — a `[Theory]` signals multiple scenarios.

```csharp
// BAD — single data row
[Theory]
[InlineData("192.168.0.0/24")]
public void Parse_ValidInput_ReturnsSubnet_Test(string input) { ... }

// GOOD — convert to [Fact]
[Fact]
public void Parse_ValidInput_ReturnsSubnet_Test() { ... }
```

---

### 2. `IEnumerable<object[]>` Instead of `TheoryData<T>`

Raw `object[]` arrays bypass type safety and make the data source's contract invisible to the reader. Refactoring the production method's signature won't produce a compile error.

```csharp
// BAD
public static IEnumerable<object[]> Parse_Test_Values()
{
    yield return new object[] { "192.168.0.0/24", 256 };
}

// GOOD
public static TheoryData<string, int> Parse_Test_Values =>
    new() { { "192.168.0.0/24", 256 } };
```

---

### 3. `ITestOutputHelper` as Assertion Substitute

Capturing output to later assert on it is a testing anti-pattern — it couples the test to an implementation detail and makes failures harder to diagnose.

```csharp
// BAD — asserting on captured output
_output.WriteLine(result.ToString());
Assert.Contains("192.168.0.0", _output.ToString());

// GOOD — assert on the actual return value
Assert.Equal("192.168.0.0", result.NetworkAddress.ToString());
```

---

### 4. `Console.WriteLine` in Tests

xUnit v3 suppresses `Console.WriteLine` by default — output goes nowhere. Use `ITestOutputHelper` if diagnostic output is genuinely needed.

```csharp
// BAD — silent output
Console.WriteLine($"Testing with input: {input}");

// GOOD — visible diagnostic output
_output.WriteLine($"Testing with input: {input}");
```

---

### 5. `Assert.True(x == y)` Instead of `Assert.Equal`

Boolean-wrapped equality checks produce opaque failure messages (`Expected: True, Actual: False`) that don't show the values involved.

```csharp
// BAD — opaque failure
Assert.True(result.Count == 3);

// GOOD — shows both values on failure
Assert.Equal(3, result.Count);
```

See [REFERENCE.md](REFERENCE.md) for the full assertion anti-patterns table.

---

### 6. `[assembly: CollectionBehavior(DisableTestParallelization = true)]`

Disabling all parallelism globally silences flaky-test symptoms without fixing the root cause. If tests share state, make the state isolated or use a named `[Collection]` for the specific classes that conflict.

```csharp
// BAD — disables parallelism globally
[assembly: CollectionBehavior(DisableTestParallelization = true)]

// GOOD — isolate only the conflicting classes
[Collection("DatabaseTests")]
public sealed class SlowDatabaseTests { ... }
```

---

### 7. `ITestOutputHelper` Without Dispose in Long-Lived Fixtures

When `ITestOutputHelper` is injected into a fixture or shared object that outlives a single test, the underlying buffer can accumulate across tests. Dispose it when the fixture is torn down.

```csharp
// BAD — output grows unbounded in a class-level fixture
public sealed class MyFixture
{
    public ITestOutputHelper Output { get; }

    public MyFixture(ITestOutputHelper output)
    {
        Output = output; // captured for the class lifetime
    }
}

// GOOD — avoid injecting ITestOutputHelper into shared fixtures.
// Use it only in per-test constructors.
```

---

### 8. `ITestOutputHelper` Injection in Static Theory Data

Theory data factory methods are static — they cannot receive `ITestOutputHelper` via constructor injection. If you need diagnostic output during data generation, move that logic into the test body or a separate helper.

```csharp
// BAD — static data source cannot access ITestOutputHelper
public static TheoryData<string> Data
{
    get
    {
        _output.WriteLine("generating data..."); // compiler error
        return new TheoryData<string> { "a", "b" };
    }
}
```

---

## Related

- [SKILL.md](SKILL.md) — xUnit-specific rules.
- [REFERENCE.md](REFERENCE.md) — API lookup tables and assertion anti-patterns table.
- [EXAMPLES.md](EXAMPLES.md) — worked examples.
- [General Quality Checklist](../../references/quality-checklist.md).
