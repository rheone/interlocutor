---
name: nunit-csharp
description: Write, update, and improve NUnit v5 unit tests in C# projects. Covers constraint-based assertions, parameterized tests, fixture lifecycle, mocking delegation, and NUnit-specific patterns. Use when writing or reviewing C# unit tests in NUnit projects.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert Engelhardt <rheone@gmail.com>
  version: 2.0.0
---

# NUnit C# Testing Skill (v5)

## Scope Selection

| Scope | Apply to |
| ----- | -------- |
| **Single test** | One `[Test]` method |
| **Group** | Related tests within one `#region` |
| **Class** | Entire test class file |
| **Namespace** | All test classes in a namespace |
| **Project** | All test classes in a project |

## Framework Checklist

Apply these NUnit-specific checks during every sweep, after the [General Quality Checklist](../../references/quality-checklist.md).

- [ ] Constraint-based `Assert.That(actual, constraint)` used, not classic `Assert.AreEqual`
- [ ] `Assert.Multiple` used for batched independent assertions in one scenario
- [ ] `[SetUp]` used for per-test setup (not the constructor — NUnit creates one instance per test)
- [ ] `[TestCase]` with exactly one row converted to `[Test]`
- [ ] `[Ignore]` includes written justification with ticket reference
- [ ] `[Parallelizable]` / `[NonParallelizable]` explicitly chosen where parallelism matters
- [ ] `[Retry]` used sparingly for flaky infrastructure interaction only

## Core Rules

### Naming

- Test method names: `{Member}_{Scenario}_{Expectation}_Test`
- Examples: `Parse_ValidCidr_ReturnsSubnet_Test`, `Parse_NullInput_ThrowsArgumentNullException_Test`

### AAA Structure

Every test follows Arrange / Act / Assert with section comments:

```csharp
// Arrange
var input = "192.168.0.0/24";

// Act
var result = Subnet.Parse(input);

// Assert
Assert.That(result.NetworkAddress, Is.EqualTo(IPAddress.Parse("192.168.0.0")));
```

### Test Method Attribute

- Use `[Test]` for standard test methods.
- Use `[TestCase(...)]` for parameterized tests with simple literal values.
- Use `[TestCaseSource(nameof(...))]` for complex or computed test data.

### Constraint-Based Assertions

Default assertion style is **constraint-based**:

```csharp
Assert.That(actual, Is.EqualTo(expected));       // preferred
// NOT Assert.AreEqual(expected, actual);         // classic style — avoid
```

See REFERENCE.md for the full constraint catalog.

### Assert.Multiple

Use `Assert.Multiple` when a single logical scenario has multiple independent properties to verify and you want all failures reported:

```csharp
Assert.Multiple(() =>
{
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(result.Value, Is.EqualTo(42));
});
```

Without `Assert.Multiple`, only the first assertion failure in a test method is reported.

### Exception Assertions

```csharp
// Sync
Assert.That(() => Subnet.Parse(null), Throws.InstanceOf<ArgumentNullException>());

// Async
Assert.That(async () => await service.ProcessAsync(null), Throws.InstanceOf<ArgumentNullException>());

// Alternative style with Assert.Throws
var ex = Assert.Throws<ArgumentNullException>(() => Subnet.Parse(null));
Assert.That(ex.Message, Does.Contain("input"));
```

### Async Tests

- Async test methods must return `Task`, never `async void`.
- Use `Assert.ThrowsAsync<T>` or `Throws.InstanceOf<T>` with async lambdas.

### Fixture Lifecycle

| Attribute | Runs |
| --------- | ---- |
| `[SetUp]` | Before each test |
| `[TearDown]` | After each test |
| `[OneTimeSetUp]` | Once before all tests in the class |
| `[OneTimeTearDown]` | Once after all tests in the class |

**Important:** NUnit creates one test class instance per test, unlike xUnit. Do not use the constructor for test setup — use `[SetUp]` instead.

### Parameterized Tests

- `[TestCase]` for simple inline data (primitives, strings, enums).
- `[TestCaseSource(nameof(...))]` for complex objects or computed data.
- Use `TestCaseData` with `.SetName()` for readable test names.
- Never write a `[TestCase]` with exactly one row — convert to `[Test]`.

### Skipping Tests

`[Ignore("reason")]` requires a written justification and a ticket reference:

```csharp
[Test]
[Ignore("Blocked by #42 — parser not yet implemented for IPv6")]
public void Parse_ValidIPv6_ReturnsSubnet_Test() { }
```

### Mocking

- Never substitute the class under test — see the detected mocking companion skill.
- Mock verification belongs in the Assert section.

### Test Isolation

- No static mutable state shared between tests.
- No `Thread.Sleep` — arrange deterministic conditions instead.

### String Input Coverage

When a method accepts `string` parameters, include test cases for: `null`, `""`, `" "`, `"\t"`, `"\n"`, `"\r"`, and a valid representative value.

### Null Parameter Testing

For every public constructor or method reference-type parameter, test that `null` throws `ArgumentNullException`.

### Private / Internal Member Testing

1. Test through the public API first.
2. Use `[assembly: InternalsVisibleTo("Tests")]` for `internal` members.
3. Use reflection only as a last resort.

### Parallelization

- `[Parallelizable(ParallelScope.Children)]` — methods within a class run in parallel.
- `[NonParallelizable]` — force sequential execution for an individual test.
- By default, NUnit runs tests within a class sequentially, classes in parallel.

### Retry

Use `[Retry(int)]` for flaky tests that interact with unreliable infrastructure:

```csharp
[Test]
[Retry(3)]
public void ExternalService_Call_ReturnsOk_Test() { }
```

### [TestFixture]

`[TestFixture]` is optional in NUnit v5 for simple test classes. Use it when:
- The class inherits from a base test class.
- The class uses constructor injection.
- The class needs `[TestFixtureSource]`.

## Verification

After writing tests, run:

```
dotnet test {project} --framework {highest-modern-tfm} --filter "FullyQualifiedName~{ClassName}" 2>&1 | tail -3
```

Fix failures before reporting complete.

## Related Skills

This skill is invoked automatically by [`csharp-test-sweep`](../../SKILL.md) when it detects NUnit in the project file. Framework-agnostic quality rules (async void, shared static state, test naming, etc.) live in the [parent quality-checklist.md](../../references/quality-checklist.md).
