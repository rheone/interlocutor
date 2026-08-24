---
name: rhinomocks-csharp-anti-patterns
description: RhinoMocks-specific anti-patterns and pitfalls. Covers record/replay misuse, mocking the SUT, over-specification, missing VerifyAll, and StructureMap.AutoMocking traps.
license: Apache-2.0
user-invocable: false
metadata:
  author: Robert Engelhardt <rheone@gmail.com>
  version: 1.1.0
---

# RhinoMocks Anti-Patterns

> Framework-agnostic anti-patterns (missing assertions, flaky tests, etc.) are covered in [`csharp-test-sweep/references/quality-checklist.md`](../../references/quality-checklist.md). This file covers RhinoMocks-specific pitfalls only.

## 1. Using Record/Replay Instead of AAA

**Problem:** Record/replay (`Expect` / `ReplayAll` / `VerifyAll`) is fragile. The `ReplayAll` call is easy to forget, and `VerifyAll` must be called explicitly or the test passes vacuously if no expectations were set up.

**Fix:** Use AAA style (`Stub` / `AssertWasCalled`) which has no mode-switching step.

```csharp
// Bad (record/replay)
mock.Expect(x => x.GetValue()).Return(42);
repository.ReplayAll();
// ... test ...
repository.VerifyAll();

// Good (AAA)
mock.Stub(x => x.GetValue()).Return(42);
// ... test ...
mock.AssertWasCalled(x => x.GetValue());
```

## 2. Mocking the Class Under Test

**Problem:** `GenerateMock<T>()` intercepts all virtual methods. When `T` is the class under test, the real method never runs — the mock returns `default(TResult)`.

**Fix:** Use a concrete test subclass that inherits without overriding.

See [Core Rule 2 in SKILL.md](SKILL.md#core-rules) for the pattern.

## 3. Over-Specification with Repeat Constraints

**Problem:** Using `.Repeat.Exactly(N)` or `.Repeat.Times(N)` on `Stub` or `Expect` makes tests brittle. An unrelated code change that adds or removes a call site breaks the test even when the behavior is correct.

**Fix:** Use `.Repeat.Once()` sparingly. Prefer `.Repeat.AtLeastOnce()` or `AssertWasCalled` with no repeat constraint. Reserve `.Repeat.Times(N)` for cases where call count is a documented behavioral contract.

```csharp
// Brittle — breaks if Save is called exactly once elsewhere
mock.Expect(x => x.Save()).Repeat.Once();

// Better — asserts it was called at least once
mock.AssertWasCalled(x => x.Save());
```

## 4. Not Calling VerifyAll in Record/Replay Mode

**Problem:** `VerifyAll` must be called to assert expectations. If omitted, the test passes even when no expectations were set — a false positive.

**Fix:** Always call `repository.VerifyAll()` in a `finally` block or, better yet, migrate to AAA style where verification is explicit per mock.

```csharp
// Dangerous — test passes even if Expect was never set up
repository.ReplayAll();
sut.DoWork();

// Safe
repository.ReplayAll();
sut.DoWork();
repository.VerifyAll();
```

## 5. StructureMap.AutoMocking Hiding Complex Dependency Configuration

**Problem:** `RhinoAutoMocker<T>` creates mocks for all constructor dependencies automatically. When a dependency needs non-trivial fake behavior (e.g., a `MemoryStream` with seeded data, or a fake `DbContext`), the auto-mocker creates a plain mock that returns defaults — causing silent failures or null-reference exceptions.

**Fix:** For complex dependencies, register a concrete fake via the container before accessing `ClassUnderTest`:

```csharp
var autoMocker = new RhinoAutoMocker<Service>(MockMode.AAA);
autoMocker.Container.Configure(x =>
    x.For<IDbContext>().Use(new FakeDbContext()));
```

## 6. Forgetting repository.ReplayAll() in Record/Replay Mode

**Problem:** Without `ReplayAll()`, the mock stays in record mode. Method calls during Act do not match expectations, and the mock returns default values without throwing. The test appears to pass but never exercises the real path.

**Fix:** Always call `repository.ReplayAll()` or `mock.Replay()` before the Act step. Or, use AAA style where this step does not exist.

## 7. No Analyzer Safety Net

**Problem:** Unlike NSubstitute or Moq (which have Roslyn analyzers that catch common mistakes at compile time), RhinoMocks has none. Tests that compile fine can fail at runtime or pass vacuously. This is a known risk of using an unmaintained library.

**Mitigation:** Run [`csharp-test-sweep`](../../SKILL.md) audits regularly. Prefer AAA style (no `ReplayAll` to forget). Prioritize migration to a maintained framework.
