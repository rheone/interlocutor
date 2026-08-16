---
name: moq-csharp
description: Write, update, and improve Moq mock setups in C# test projects. Covers the abstract-class interception trap, argument matchers, and call verification. Use when writing or reviewing Moq usage in any C# test project regardless of test framework.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert Engelhardt <rheone@gmail.com>
  version: 2.0.0
---

# Moq C# Mocking Skill

## Framework Checklist

Apply these Moq-specific checks during every sweep, after the [General Quality Checklist](../../references/quality-checklist.md).

- [ ] `new Mock<T>()` used only for dependencies, not the class under test
- [ ] Concrete subclass used instead of `Mock<T>` when testing abstract class methods
- [ ] `mock.Object` only used when injecting into the subject under test
- [ ] Verification (`mock.Verify`) placed in Assert section, not scattered
- [ ] `It.IsAny<T>` for `CancellationToken` when cancellation is not the subject
- [ ] `It.Is<T>(predicate)` used over `It.IsAny<T>` when argument value matters
- [ ] `MockBehavior.Loose` as default; `Strict` only when interaction enforcement is needed
- [ ] Moq.Analyzers NuGet package added to the test project

## Core Rules

- `new Mock<T>()` is for **dependencies**, not the **subject under test**.
- **Never mock the class under test** — `Mock<T>` intercepts virtual methods on `T`. If `T` is the class under test, the method under test never executes and the mock returns the type default instead.
- If the method under test lives on an abstract base class, instantiate a **concrete subclass** that inherits the implementation without overriding it.
- Mock verification (`mock.Verify`) belongs in the Assert section only.
- Use `mock.Object` only when injecting the mock into another class as a dependency.
- Prefer mocking interfaces over concrete classes. When mocking a concrete class, pass constructor arguments explicitly to avoid default-value surprises.

## Anti-Pattern: Mocking the Class Under Test

`new Mock<T>()` creates a dynamic proxy that intercepts **all virtual/abstract** members on `T`. When `T` is the class under test, the method under test never runs — the test always passes vacuously.

```csharp
// BROKEN: AbstractProcessor.Process is virtual, never executes
var mock = new Mock<AbstractProcessor>();
var result = mock.Object.Process("input");  // returns default — real code never runs
Assert.Equal("expected", result);            // fails or passes vacuously
```

```csharp
// FIXED: concrete subclass inherits the implementation
var sut = new ConcreteProcessor();
var result = sut.Process("input");
Assert.Equal("expected", result);
```

**Exception**: `Mock<AbstractClass>` is correct when the abstract class is a **dependency** being injected into the subject under test, not the subject itself.

## Creating Mocks

| Scenario | Code |
|----------|------|
| Interface | `var mock = new Mock<IFoo>();` |
| Concrete class (no-arg ctor) | `var mock = new Mock<Foo>();` |
| Concrete class (with args) | `var mock = new Mock<Foo>(arg1, arg2);` |
| LINQ mock (simple cases) | `var foo = Mock.Of<IFoo>(x => x.Name == "bar");` |

## MockBehavior

| Mode | Behavior | Use |
|------|----------|-----|
| `Loose` (default) | Returns type defaults (`null`, `0`, `""`) for unsetup members | Most tests — only set up what matters |
| `Strict` | Throws `MockException` on any unsetup call | Interaction testing where every call must be intentional |

Use `Loose` as the default. Reserve `Strict` for cases where you must enforce that no unexpected calls occur.

## Setup Patterns

```csharp
// Returns a value
mock.Setup(x => x.Method("arg")).Returns("result");

// Async — returns Task<T>
mock.Setup(x => x.AsyncMethod()).ReturnsAsync("result");

// Throws
mock.Setup(x => x.Method("bad")).Throws<InvalidOperationException>();
mock.Setup(x => x.Method("bad")).Throws(new InvalidOperationException("msg"));

// Property getter
mock.SetupGet(x => x.Name).Returns("Alice");

// Property setter verification target
mock.SetupSet(x => x.Name = It.IsAny<string>());

// Auto-stub property (get/set without explicit setup)
var mock = new Mock<IFoo>();
mock.SetupProperty(x => x.Name, "default");
```

## Argument Matchers

| Matcher | Usage |
|---------|-------|
| `It.IsAny<T>()` | Match any value of type `T` |
| `It.Is<T>(p)` | Match values satisfying predicate `p` |
| `It.IsInRange<T>(lo, hi, Range)` | Match inclusive/exclusive range |
| `It.IsRegex(pattern)` | Match string against regex |
| `It.IsNotNull<T>()` | Match any non-null value |

Use `It.IsAny<T>()` for `CancellationToken` when cancellation is not the subject. Use `It.Is<T>(p)` when the exact argument value matters for correctness.

## Verification

```csharp
// Call was made exactly once
mock.Verify(x => x.Method("arg"), Times.Once());

// Call was never made
mock.Verify(x => x.Method(It.IsAny<string>()), Times.Never());

// Call was made at least once
mock.Verify(x => x.Method("arg"), Times.AtLeastOnce());

// With custom failure message
mock.Verify(x => x.Method("arg"), "Save was not called when expected");
```

### Times

| Member | Meaning |
|--------|---------|
| `Times.Once()` | Exactly 1 call |
| `Times.Never()` | Exactly 0 calls |
| `Times.Exactly(n)` | Exactly `n` calls |
| `Times.AtLeastOnce()` | 1 or more |
| `Times.AtLeast(n)` | `n` or more |
| `Times.AtMost(n)` | `n` or fewer |
| `Times.Between(a, b, Range)` | Between `a` and `b` inclusive/exclusive |

## Callbacks

```csharp
// Capture arguments as they are called
var captured = "";
mock.Setup(x => x.Method(It.IsAny<string>()))
    .Callback<string>(s => captured = s);

// Execute logic on call (e.g., count invocations)
int callCount = 0;
mock.Setup(x => x.Method())
    .Callback(() => callCount++);
```

## SetupSequence

```csharp
mock.SetupSequence(x => x.GetStatus())
    .Returns("Pending")
    .Returns("Approved")
    .Returns("Completed");
```

## Capture with Capture.In

```csharp
var capturedArgs = new List<string>();
mock.Setup(x => x.Method(Capture.In(capturedArgs)));
// After exercise: assert on capturedArgs
```

## Mock.Of<T> — LINQ to Mocks

```csharp
var repo = Mock.Of<IRepository>(r =>
    r.Find(1) == new User { Id = 1, Name = "Alice" } &&
    r.Find(2) == new User { Id = 2, Name = "Bob" });
```

Use for simple, read-only arrangements. Switch to `new Mock<T>()` when you need verification, callbacks, or sequences.

## Reset Invocations

```csharp
mock.Invocations.Clear();  // reset call history without losing setups
```
Useful in test setup when multiple tests share a mock but each needs a clean call count.

## Moq Analyzers

Add the [Moq.Analyzers](https://www.nuget.org/packages/Moq.Analyzers/) NuGet package to catch common mistakes (incorrect setup signatures, mismatched return types) at build time rather than at test runtime.

## Related Skills

This skill is invoked automatically by [`csharp-test-sweep`](../../SKILL.md) when it detects Moq in the project file.

See also:
- [REFERENCE.md](REFERENCE.md) — API lookup tables
- [EXAMPLES.md](EXAMPLES.md) — worked examples
- [ANTI-PATTERNS.md](ANTI-PATTERNS.md) — framework-specific pitfalls
