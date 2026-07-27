---
name: xunit-csharp-reference
description: API lookup tables and reference patterns for xUnit v3 C# testing
license: Apache-2.0
metadata:
  author: Robert Engelhardt <rheone@gmail.com>
  version: 1.1.0
---

# xUnit v3 Reference

> See [SKILL.md](SKILL.md) for rules, [EXAMPLES.md](EXAMPLES.md) for worked examples, and [ANTI-PATTERNS.md](ANTI-PATTERNS.md) for pitfalls.

---

## Non-Serializable Types in Theories

When a type used in `TheoryData<T>` is not natively serializable by xUnit, implement `IXunitSerializer<T>` and register it:

```csharp
internal sealed class SubnetXunitSerializer : IXunitSerializer<Subnet>
{
    public Subnet Deserialize(string serialized) => Subnet.Parse(serialized);
    public string Serialize(Subnet value) => value.ToString();
    public bool IsSerializable(Type type, object? value, out string? failureReason)
    {
        failureReason = type != typeof(Subnet) ? $"{type.FullName} not supported" : null;
        return type == typeof(Subnet);
    }
}

[assembly: RegisterXunitSerializer(typeof(SubnetXunitSerializer), typeof(Subnet))]
```

See [references/IXunitSerializer.md](references/IXunitSerializer.md) for the full pattern including `IXunitSerializable` (v2) → `IXunitSerializer` (v3) migration, multi-type serializers, and round-trip rules.

---

## Assertion Anti-Patterns Table

| Anti-pattern | Replace with |
|---|---|
| `Assert.True(x == y)` | `Assert.Equal(y, x)` |
| `Assert.True(x != y)` | `Assert.NotEqual(y, x)` |
| `Assert.True(result != null)` | `Assert.NotNull(result)` |
| `Assert.True(result == null)` | `Assert.Null(result)` |
| `Assert.Equal(true, condition)` | `Assert.True(condition)` |
| `Assert.Equal(false, condition)` | `Assert.False(condition)` |
| `Assert.Equal(null, obj)` | `Assert.Null(obj)` |
| `Assert.NotEqual(null, obj)` | `Assert.NotNull(obj)` |

Specific assertions produce significantly better failure messages than boolean wrappers.

---

## Failure Messages

Add a message only when the default failure output would be ambiguous — same assertion appears multiple times in one test, or the asserted value doesn't reveal the failure cause:

```csharp
Assert.True(result.IsValid, "IsValid should be true after successful parse");
```

Never add a message that merely restates the assertion.

---

## Fixture Decision Table

| Situation | Use |
|---|---|
| Cheap setup, no I/O | Constructor / `IDisposable` |
| Async setup or teardown | `IAsyncLifetime` |
| Expensive, safe to share per-class | `IClassFixture<T>` |
| Shared across test classes | `ICollectionFixture<T>` + `[Collection]` |
| Each test needs clean state | Constructor / `IDisposable` always |

See [references/Fixtures.md](references/Fixtures.md) for full wiring examples, collection definitions, `IAsyncLifetime` pattern, and what NOT to do.

---

## Analyzer Recommendation

Add the [`xunit.analyzers`](https://www.nuget.org/packages/xunit.analyzers/) NuGet package to your test project. It detects:

- Incorrect assertion argument ordering (`Assert.Equal(actual, expected)`)
- Async test methods missing `async` keyword
- `[Fact]` on methods returning `void` when they should return `Task`
- `ITestOutputHelper` injected but not used

Install: `dotnet add package xunit.analyzers`

---

## xUnit v3 API Quick Reference

| API | Purpose |
|---|---|
| `[Fact]` | Single scenario test |
| `[Theory]` | Parameterized test |
| `[MemberData(nameof(...))]` | Strongly-typed data source |
| `[InlineData(...)]` | Inline primitive literals |
| `TheoryData<T1, T2, ...>` | Strongly-typed data container |
| `Assert.Equal(expected, actual)` | Value equality |
| `Assert.NotEqual(expected, actual)` | Value inequality |
| `Assert.True(condition)` | Boolean true |
| `Assert.False(condition)` | Boolean false |
| `Assert.Null(obj)` / `Assert.NotNull(obj)` | Null checks |
| `Assert.Throws<T>(fn)` / `Assert.ThrowsAsync<T>(fn)` | Exception assertion |
| `Assert.Multiple(params Action[])` | Batch independent assertions |
| `Assert.Equivalent(expected, actual)` | Structural deep-compare |
| `Assert.Contains(expected, collection)` | Collection membership |
| `Assert.DoesNotContain(expected, collection)` | Collection non-membership |
| `Assert.Empty(collection)` | Empty collection |
| `Assert.Single(collection)` | Exactly one element |
| `Assert.All(collection, action)` | Assertion on every element |
| `Assert.IsType<T>(obj)` | Exact type match |
| `Assert.IsAssignableFrom<T>(obj)` | Type compatibility |
| `IClassFixture<T>` | Shared fixture per class |
| `ICollectionFixture<T>` | Shared fixture per collection |
| `IAsyncLifetime` | Async init/teardown |
| `ITestOutputHelper` | Diagnostic output |
| `[Trait("name", "value")]` | Test metadata / filtering |
| `[Skip]` | Skip with justification |
| `[assembly: RegisterXunitSerializer]` | Theory serializer registration |

---

## Related

- [SKILL.md](SKILL.md) — xUnit-specific rules.
- [EXAMPLES.md](EXAMPLES.md) — worked examples.
- [ANTI-PATTERNS.md](ANTI-PATTERNS.md) — framework-specific pitfalls.
- [General Quality Checklist](../../references/quality-checklist.md).
