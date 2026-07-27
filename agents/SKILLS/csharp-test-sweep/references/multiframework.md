# Multi-Framework Reference

## Predefined Symbols

SDK-style projects define these symbols automatically based on `<TargetFrameworks>`:

| Symbol | When defined |
|--------|-------------|
| `NET48` | `net48` |
| `NETFRAMEWORK` | Any `net4x` |
| `NETSTANDARD2_0` | `netstandard2.0` |
| `NETSTANDARD2_1` | `netstandard2.1` |
| `NETSTANDARD2_1_OR_GREATER` | `netstandard2.1` and above |
| `NET8_0` | `net8.0` |
| `NET8_0_OR_GREATER` | `net8.0` and above |
| `NET9_0_OR_GREATER` | `net9.0` and above |
| `NET10_0_OR_GREATER` | `net10.0` and above |

The full set follows the pattern `NET{major}_{minor}_OR_GREATER`. Symbols are cumulative — `net10.0` defines `NET8_0_OR_GREATER`, `NET9_0_OR_GREATER`, and `NET10_0_OR_GREATER`.

## Reading Target Frameworks from the Project File

```xml
<PropertyGroup>
  <TargetFrameworks>net48;net8.0;net9.0;net10.0</TargetFrameworks>
</PropertyGroup>
```

or

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
</PropertyGroup>
```

Check for `net4` or `netstandard` prefix in `<TargetFrameworks>` to determine whether environment gating is needed.

## When to Use `#if` vs `[Skip]` vs Separate Test Method

| Situation | Approach |
|-----------|----------|
| API exists on all targets but behaves differently | `#if` guard around the assertion that differs |
| API only exists on some targets | `#if` guard around the entire test method |
| Test infrastructure unavailable on a target (e.g., `BinaryFormatter` on `net8+`) | `#if` guard around the test |
| Test is slow or broken only on one target | `[Skip]` with justification — do not use `#if` to hide it |
| Behavior difference is so significant a separate test is clearer | Separate method with `#if` to exclude the other |

**Never duplicate a test for each target.** If the only difference is a type name or a constant value, use `#if` to swap that value, not to copy the whole test.

### Example: Framework-Specific Import and Test

```csharp
#if NET48
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
#endif

[Theory]
[InlineData(typeof(IComparable))]
[InlineData(typeof(IEquatable<Subnet>))]
#if NET48
[InlineData(typeof(ISerializable))]   // BinaryFormatter only available on net48
#endif
public void Assignability_Test(Type assignableFromType)
{
    // Assert
    Assert.True(assignableFromType.IsAssignableFrom(typeof(Subnet)));
}
```

### Example: Conditional Assertion

```csharp
[Fact]
public void ToString_ReturnsExpectedFormat_Test()
{
    // Arrange
    var subnet = Subnet.Parse("192.168.0.0/24");

    // Act
    var result = subnet.ToString();

    // Assert
#if NET8_0_OR_GREATER
    Assert.Equal("192.168.0.0/24", result, StringComparer.Ordinal);
#else
    Assert.Equal("192.168.0.0/24", result);
#endif
}
```

## Batching .NET Framework Incompatible Test Runs

When the environment has no working .NET Framework test runner, collect all classes modified during the sweep and emit a single command block at the end:

```shell
# Run these in an environment with .NET Framework support:
dotnet test ./src/MyLib.Tests --framework net48 --filter "FullyQualifiedName~SubnetTests"
dotnet test ./src/MyLib.Tests --framework net48 --filter "FullyQualifiedName~IPAddressRangeTests"
# ... one line per modified class
```

Or to run the full project on a single framework:

```shell
dotnet test ./src/MyLib.Tests --framework net48
```

Provide the full path to the test project so the user can run it from any working directory.

## Lowest Common Denominator Rule

Write tests at the lowest common denominator target by default. Only introduce `#if` guards when:

1. The API under test does not exist on all targets
2. The assertion helper or type used in the test does not exist on all targets
3. A framework behavior difference would cause the test to fail on a legitimate target

Do not add `#if` guards "just in case." If you are unsure whether a difference exists, write the test without guards first and note the uncertainty with `// SWEEP-AMBIGUITY:`.

## C# Language Feature Version Table

When the test project targets `net4x`, the compiler defaults to C# 7.3. Features introduced after C# 7.3 will compile on modern TFMs but fail with a `CS8370` (or similar) error on `net48`. Write at the lowest C# version in the `<TargetFrameworks>` list.

| Feature | Minimum C# | Fails on `net48`? | Fix for `net48` |
|---------|-----------|-------------------|-----------------|
| Inferred delegate type (`var act = () => ...`) | C# 10 | Yes | Use `Action act = ...` or `Func<T> act = ...` |
| Target-typed `new()` without type name | C# 9 | Yes | Write the full `new TheoryData<T1, T2>()` |
| `record` types | C# 9 | Yes | Use a plain `class` |
| `init`-only setters | C# 9 | Yes | Use a constructor parameter |
| Switch expressions (`x switch { ... }`) | C# 8 | Yes | Use a `switch` statement |
| Nullable reference type annotations (`string?`) | C# 8 | Yes (warning) | Omit annotations; use `#nullable disable` if needed |
| `using` declarations (without braces) | C# 8 | Yes | Use `using (...) { }` block |
| Default interface members | C# 8 | Yes | Not applicable in test code |

**When in doubt**: write the feature, run `dotnet build --framework net48`, and let the compiler tell you. Do not speculatively guard features that compile cleanly on all targets.
