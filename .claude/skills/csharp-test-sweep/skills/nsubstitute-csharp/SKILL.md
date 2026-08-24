---
name: nsubstitute-csharp
description: Write, update, and improve NSubstitute mock setups in C# test projects. Covers the abstract-class interception trap, partial substitutes, argument matchers, and call verification. Use when writing or reviewing NSubstitute usage in any C# test project regardless of test framework.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert Engelhardt <rheone@gmail.com>
  version: 2.0.0
---

# NSubstitute C# Mocking Skill

## Framework Checklist

Apply these NSubstitute-specific checks during every sweep, after the [General Quality Checklist](../../references/quality-checklist.md).

- [ ] `Substitute.For<T>()` used only for dependencies, not the class under test
- [ ] Concrete subclass used instead of substitute when testing abstract class methods
- [ ] `Received` / `DidNotReceive` verification placed in Assert section
- [ ] `Arg.Any<T>` used sparingly — `CancellationToken` or irrelevant args only
- [ ] `Arg.Is<T>(predicate)` used when argument value matters for correctness
- [ ] `Substitute.ForPartsOf<T>` used sparingly (indicates design should be improved)
- [ ] NSubstitute.Analyzers NuGet package added to the test project

## Core Rules

- `Substitute.For<T>()` is for **dependencies**, not the **subject under test**.
- **Never substitute the class under test** — if the method under test is virtual, NSubstitute intercepts it and returns the type default instead of running the real implementation.
- If the method under test lives on an abstract base class, instantiate a **concrete subclass** that inherits the implementation without overriding it.
- `Arg.Any<T>()` is permitted only for: `CancellationToken` when cancellation is not the subject; complex objects verified by a separate assertion; call-count/ordering tests where argument values are irrelevant.
- Mock verification (`Received`, `DidNotReceive`) belongs in the Assert section.

## Anti-Pattern: Substituting the Class Under Test

**Never use `Substitute.For<T>()` when `T` is the class whose behavior you are testing.** NSubstitute intercepts all virtual methods and returns the type default — the real implementation never runs, producing vacuously passing tests.

```csharp
// BROKEN: NSubstitute intercepts virtual ToString → returns ""; test passes vacuously
var range = Substitute.For<AbstractIPAddressRange>(head, tail);
var result = range.ToString("G", CultureInfo.CurrentCulture); // always ""
Assert.Equal("192.168.1.1 - 192.168.1.42", result);

// FIXED: concrete subclass inherits the implementation
var range = new IPAddressRange(head, tail);
var result = range.ToString("G", CultureInfo.CurrentCulture); // real implementation runs
Assert.Equal("192.168.1.1 - 192.168.1.42", result);
```

See [`ANTI-PATTERNS.md`](ANTI-PATTERNS.md) for this and 6 other framework-specific pitfalls.

## When `Substitute.For<AbstractClass>` Is Correct

Substituting an abstract class is appropriate when:

- The abstract class is a **dependency** being injected into the class under test
- You are testing **interactions** (verifying the subject calls a method on the dependency), not the dependency's behavior
- The method you care about is **abstract** (not virtual with a body) — NSubstitute cannot intercept abstract methods that have no implementation, so a substitute is the only option

```csharp
// Correct: substituting a dependency, not the subject under test
var dependency = Substitute.For<AbstractProcessor>();
dependency.Process(Arg.Any<string>()).Returns("ok");

var sut = new MyService(dependency);
sut.Run("input");

dependency.Received(1).Process("input");
```

## Partial Substitutes

When you need both the real behavior **and** call verification, use `Substitute.ForPartsOf<T>()` (NSubstitute partial substitute). This calls through to the real implementation unless explicitly configured:

```csharp
var partial = Substitute.ForPartsOf<ConcreteService>();
partial.When(x => x.VirtualMethod()).DoNotCallBase(); // suppress only this call

// All other virtual methods call through to the real implementation
```

Use partial substitutes sparingly — they indicate the design may benefit from extracting the dependency rather than partially mocking the subject.

## NSubstitute API Landmarks

| Concern | NSubstitute API |
|---------|-----------------|
| Create substitute | `Substitute.For<IFoo>()` |
| Stub return value | `substitute.Method(arg).Returns(value);` |
| Stub with callback | `substitute.Method(arg).Returns(x => compute(x.Arg<T>()));` |
| Verify call | `substitute.Received(1).Method(arg);` |
| Verify no call | `substitute.DidNotReceive().Method(Arg.Any<T>());` |
| Argument matcher | `Arg.Any<T>()`, `Arg.Is<T>(v => ...)` |
| Partial substitute | `Substitute.ForPartsOf<ConcreteClass>()` |
| Suppress base call | `.When(x => x.Method()).DoNotCallBase();` |


## Related Skills

This skill is invoked automatically by [`csharp-test-sweep`](../../SKILL.md) when it detects NSubstitute in the project file.
