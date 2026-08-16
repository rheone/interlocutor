---
name: justmock-csharp
description: Write, update, and improve Telerik JustMock mock setups in C# test projects. Covers the free/elevated mode split, virtual-only interception in free mode, profiler-based interception in elevated mode, argument matchers, and call verification. Use when writing or reviewing JustMock usage in any C# test project regardless of test framework.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert Engelhardt <rheone@gmail.com>
  version: 2.0.0
---

# Telerik JustMock C# Mocking Skill

## Free vs Elevated Mode

JustMock has two distinct modes. The API surface is identical in both — the difference is **what can be intercepted**:

| Mode | License | How it works | Can mock |
|------|---------|-------------|----------|
| **Free** | None (open source) | Dynamic proxy (Castle.Core) | Interfaces, abstract members, virtual members only |
| **Elevated** | Commercial license | Profiler (CLR instrumentation) | Everything: non-virtual, static, sealed, private, `DateTime.Now`, etc. |

**Free mode** has the same capabilities and constraints as Moq or NSubstitute. **Elevated mode** requires a commercial Telerik license and the JustMock profiler (`Telerik.JustMock.dll` with the profiler enabled via `Telerik.JustMock.exe` or environment variables).

**All examples in this skill work in Free mode unless explicitly marked `[Elevated]`.**

## Framework Checklist

Apply these JustMock-specific checks during every sweep, after the [General Quality Checklist](../../references/quality-checklist.md).

- [ ] `Mock.Create<T>` used only for dependencies, not the class under test
- [ ] Concrete subclass used instead of mock when testing abstract class methods
- [ ] `Mock.Assert` with explicit `Occurs` used (not inferred from setup)
- [ ] Free mode constraints respected (virtual-only) unless Elevated license is confirmed
- [ ] Elevated mode features only used with a commercial license + profiler available
- [ ] `Arg.IsAny<T>` used for `CancellationToken` when cancellation is not the subject
- [ ] `Mock.CreateLike` considered for auto-stubbing properties

## Core Rules

- `Mock.Create<T>()` is for **dependencies**, not the **subject under test**.
- **Never mock the class under test** — in Free mode, `Mock.Create<T>()` intercepts virtual methods; in Elevated mode it can intercept any method. Either way, the method under test never executes real logic.
- If the method under test lives on an abstract base class, instantiate a **concrete subclass** that inherits the implementation without overriding it.
- Mock verification (`Mock.Assert`) belongs in the Assert section only.
- Prefer mocking interfaces over concrete classes. When mocking a concrete class in Free mode, only virtual members are interceptable.

## Anti-Pattern: Mocking the Class Under Test

In **Free mode**, `Mock.Create<T>()` intercepts virtual members on `T`. If `T` is the class under test, the method under test never executes — the test returns the type default instead.

In **Elevated mode**, even non-virtual methods are intercepted — making this trap harder to spot because the mock works silently.

**Fixed — use a concrete subclass (works in both modes):**

```csharp
// BROKEN (free mode): AbstractProcessor.Process is virtual, never executes
var sut = Mock.Create<AbstractProcessor>();
sut.Process("input"); // returns default — real code never runs

// FIXED: concrete subclass inherits the implementation
var sut = new ConcreteProcessor();
var result = sut.Process("input");
Assert.Equal("expected", result);
```

**Elevated mode note**: if `Mock.Create<ConcreteProcessor>()` is used in Elevated mode, all methods (virtual or not) are interceptable. Always verify the actual implementation runs by checking the test result against expected behavior, not just call counts.

## Creating Mocks

| Scenario | Free mode | Elevated mode |
|----------|-----------|---------------|
| Interface | `Mock.Create<IFoo>()` | Same API |
| Abstract class (virtual members) | `Mock.Create<AbstractBase>()` | Same API |
| Concrete class (virtual only) | `Mock.Create<Foo>()` | Same API — non-virtual won't intercept |
| Sealed / static / non-virtual | ❌ Runtime failure | `Mock.Create<SealedClass>()` |
| Static class mocking | ❌ | `Mock.SetupStatic<StaticService>()` |

## Behavior Modes

| Mode | Behavior | Free | Elevated |
|------|----------|------|----------|
| `Loose` (default) | Returns type defaults (`null`, `0`, `""`) for unarranged calls | Yes | Yes |
| `Strict` | Throws `MockException` on any unarranged call | Yes | Yes |

## Setup and Verification Patterns

```csharp
// Arrange — set up a dependency
Mock.Arrange(() => dependency.Method("arg")).Returns("result");

// Assert — verify a call was made
Mock.Assert(() => dependency.Method("arg"), Occurs.Once());

// Assert — verify a call was never made
Mock.Assert(() => dependency.Method(Arg.IsAny<string>()), Occurs.Never());
```

## Mock.CreateLike

Auto-stubs all properties on `T` with default values (empty strings, zeros, etc.) — no explicit arrangement needed for property access:

```csharp
var mock = Mock.CreateLike<IFoo>();
// mock.Name is "", mock.Count is 0 — no Arrange needed for properties
```

## Argument Matchers

| Matcher | Usage |
|---------|-------|
| `Arg.IsAny<T>()` | Match any value of type `T` |
| `Arg.Matches<T>(predicate)` | Match values satisfying predicate |
| `Arg.AnyString` | Match any string |
| `Arg.IsInRange<T>(lo, hi, Range)` | Match inclusive/exclusive range |

Use `Arg.IsAny<T>()` for `CancellationToken` when cancellation is not the subject.

## Call Counts (Occurs)

| Member | Meaning |
|--------|---------|
| `Occurs.Once()` | Exactly 1 call |
| `Occurs.Never()` | Exactly 0 calls |
| `Occurs.Exactly(n)` | Exactly `n` calls |
| `Occurs.AtLeast(n)` | `n` or more |
| `Occurs.AtMost(n)` | `n` or fewer |

## Callbacks

```csharp
string captured = null;
Mock.Arrange(() => mock.Method(Arg.IsAny<string>()))
    .DoInstead((string s) => captured = s);
```

## Assert Multiple

```csharp
Mock.AssertMultiple(() =>
{
    Mock.Assert(() => mock.Save(customer), Occurs.Once());
    Mock.Assert(() => mock.Notify(customer), Occurs.Once());
});
```

## Related Skills

This skill is invoked automatically by [`csharp-test-sweep`](../../SKILL.md) when it detects JustMock in the project file.

Framework-agnostic quality rules (async void, shared static state, test naming, etc.) live in the [parent quality-checklist.md](../../references/quality-checklist.md).

See also:
- [REFERENCE.md](REFERENCE.md) — API lookup tables
- [EXAMPLES.md](EXAMPLES.md) — worked examples
- [ANTI-PATTERNS.md](ANTI-PATTERNS.md) — framework-specific pitfalls
