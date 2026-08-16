---
name: rhinomocks-csharp
description: Write, update, and improve RhinoMocks mock setups in legacy C# test projects. Covers AAA style, StructureMap.AutoMocking, the abstract-class interception trap, and call verification. Use when writing or reviewing RhinoMocks usage in any C# test project regardless of test framework.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert Engelhardt <rheone@gmail.com>
  version: 2.0.0
---

# RhinoMocks C# Mocking Skill

> **WARNING: RhinoMocks is unmaintained as of 2020.** Do not use in new projects. This skill exists to maintain and improve legacy test suites. When modifying an existing RhinoMocks project, flag migration to [NSubstitute](https://nsubstitute.github.io/) or [Moq](https://github.com/devlooped/moq) as a sweep finding.

This skill is invoked automatically by [`csharp-test-sweep`](../../SKILL.md) when it detects RhinoMocks in the project file. Framework-agnostic quality rules live in [`csharp-test-sweep/references/quality-checklist.md`](../../references/quality-checklist.md).

## Framework Checklist

Apply these RhinoMocks-specific checks during every sweep, after the [General Quality Checklist](../../references/quality-checklist.md).

- [ ] AAA style used (`GenerateMock` / `Stub` / `AssertWasCalled`), not record/replay
- [ ] `GenerateMock<T>` / `GenerateStub<T>` used only for dependencies, not the class under test
- [ ] Concrete subclass used instead of mock when testing abstract class methods
- [ ] `AssertWasCalled` / `AssertWasNotCalled` placed in Assert section
- [ ] StructureMap.AutoMocking / RhinoAutoMocker used with awareness of hidden dependencies
- [ ] Existing record/replay tests flagged for migration to AAA
- [ ] Project flagged for migration to NSubstitute or Moq

## Core Rules

### 1. AAA Style Is Preferred

Use Arrange-Act-Assert (AAA) style with `MockRepository.GenerateMock<T>()`, `.Stub()`, and `.AssertWasCalled()`. Do not use record/replay model except when maintaining existing tests that already use it.

```csharp
var mock = MockRepository.GenerateMock<IDependency>();
mock.Stub(x => x.GetValue()).Return(42);
var sut = new Service(mock);
var result = sut.DoWork();
Assert.Equal(42, result);
mock.AssertWasCalled(x => x.GetValue());
```

### 2. Never Mock the Class Under Test

`GenerateMock<T>()` and `GenerateStub<T>()` intercept **all virtual methods** on `T`. If `T` is the class under test, the method under test never executes — the mock returns the type default instead.

**Bad:**
```csharp
var sut = MockRepository.GenerateMock<AbstractProcessor>();
```

**Good — concrete subclass:**
```csharp
private class TestableProcessor : AbstractProcessor
{
    // inherit without overriding the method under test
}
var sut = new TestableProcessor();
```

### 3. Mock Verification Belongs in Assert Section

`AssertWasCalled` and `AssertWasNotCalled` are assertions about interaction, not setup. Place them after the Act step.

### 4. StructureMap.AutoMocking / RhinoAutoMocker

When the project uses `RhinoAutoMocker<T>` (from `StructureMap.AutoMocking`), it auto-creates mocks for all constructor dependencies. Use it for rapid setup, but be aware it hides dependency configuration from casual readers.

```csharp
var autoMocker = new RhinoAutoMocker<Service>(MockMode.AAA);
autoMocker.Get<IDependency>().Stub(x => x.GetValue()).Return(42);
var sut = autoMocker.ClassUnderTest;
```

### 5. Record/Replay Model Is Deprecated

Record/replay (`Expect` / `ReplayAll` / `VerifyAll`) was the original RhinoMocks API. It is more fragile and harder to read than AAA. Only use when maintaining existing record/replay tests. Do not write new tests with it.

### 6. Concrete Subclass for Abstract Classes

When the class under test inherits from an abstract base, create a minimal concrete subclass that does not override the method under test. This lets the real implementation execute while still allowing the test to instantiate the type.

### 7. Migration Pathway

When touching a RhinoMocks test file, consider migrating to NSubstitute or Moq:

| RhinoMocks                        | NSubstitute                     | Moq                                  |
| --------------------------------- | ------------------------------- | ------------------------------------ |
| `GenerateMock<T>()`               | `Substitute.For<T>()`           | `new Mock<T>()`                      |
| `stub.Stub(x => x.M()).Ret(v)`    | `sub.Method().Returns(v)`       | `mock.Setup(x => x.M()).Returns(v)`  |
| `mock.AssertWasCalled(x => x.M())`| `sub.Received().Method()`       | `mock.Verify(x => x.M())`            |
| `Arg<T>.Is.Equal(v)`              | `Arg.Is<T>(x => x == v)`        | `It.Is<T>(x => x == v)`              |

## Related Skills

| Skill | Purpose |
|-------|---------|
| [`csharp-test-sweep`](../../SKILL.md) | Orchestrates the full test sweep; invokes this skill |
| [`csharp-test-sweep/references/quality-checklist.md`](../../references/quality-checklist.md) | Framework-agnostic quality rules applied at every class |
| [`REFERENCE.md`](REFERENCE.md) | RhinoMocks API lookup tables |
| [`EXAMPLES.md`](EXAMPLES.md) | Full worked examples |
| [`ANTI-PATTERNS.md`](ANTI-PATTERNS.md) | Framework-specific pitfalls |
