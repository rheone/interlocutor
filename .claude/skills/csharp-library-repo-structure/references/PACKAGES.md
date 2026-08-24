# Recommended packages for .NET library repos

During bootstrap, the skill asks which packages you want. Each category below
lists quality options with the recommended default pre-selected.

See [DOTNET-CLI.md](DOTNET-CLI.md) for the `dotnet add package` commands.
See [TEMPLATES.md](TEMPLATES.md) for how `Directory.Packages.props.tmpl` is
populated from your selections.

---

## 1. Analyzers (shared across all projects)

| Package | Description | Recommended |
|---|---|---|
| `StyleCop.Analyzers` | Code style enforcement (needs `stylecop.json`) | Yes |
| `Roslynator.Analyzers` | Hundreds of additional code quality rules | Yes |
| `SonarAnalyzer.CSharp` | Static analysis, bug/security detection | Yes |
| `AsyncFixer` | Async/await anti-pattern detection | Yes |
| `Microsoft.VisualStudio.Threading.Analyzers` | Threading best-practice analyzers | Yes |

---

## 2. Library (core dependencies)

| Package | Description | Recommended |
|---|---|---|
| `Microsoft.SourceLink.GitHub` | Embed source in .pdb for NuGet debugging | Yes |
| `Microsoft.Bcl.HashCode` | `HashCode` type for netstandard2.0 target | Auto* |

*Only if `netstandard2.0` is in target frameworks.

---

## 3. Test framework (pick one)

| Package | Description | Recommended |
|---|---|---|
| `xunit.v3` | Modern, extensible, widely adopted | Yes |
| `xunit.analyzers` | Catches common xUnit mistakes | With xunit |
| `xunit.runner.visualstudio` | VS/VS Code test discovery | With xunit |
| `NUnit` + `NUnit.Analyzers` | Mature, constraint-based assertions | |
| `MSTest.Sdk` | Microsoft's framework, improving fast | |

The required `Microsoft.NET.Test.Sdk` package is added regardless of choice.

---

## 4. Mocking library (pick one)

| Package | Description | Recommended |
|---|---|---|
| `NSubstitute` | Clean, minimal API, no abstract-class traps | Yes |
| `NSubstitute.Analyzers.CSharp` | Catches misused mock setups | With NSubstitute |
| `Moq` | Most popular, fluent API | |
| `FakeItEasy` | Convention-based, "natural" mocking | |

---

## 5. Test utilities (optional quality-of-life)

| Package | Description | Recommended |
|---|---|---|
| `coverlet.collector` | Code coverage collection + reporting | Yes |
| `FluentAssertions` | Readable, chainable assertions | |
| `Shouldly` | Assertion library, `x.ShouldBe(y)` style | |
| `AutoFixture` | Auto-generate test data, reduce arrange code | |
| `Bogus` | Realistic fake data generation | |
| `Verify` | Snapshot testing (golden files) | |

---

## 6. Benchmarks

| Package | Description | Recommended |
|---|---|---|
| `BenchmarkDotNet` | Industry-standard .NET microbenchmarking | Yes |

Only added if the `benchmarks` optional group is selected.

---

## 7. .NET local tools

Defined in `.config/dotnet-tools.json`. Restore with `dotnet tool restore`.

| Tool | Description | Recommended |
|---|---|---|
| `csharpier` | Opinionated C# formatter | Yes |
| `dotnet-outdated-tool` | Check for outdated NuGet packages | Yes |
| `husky` | Git hooks runner for dotnet tasks | With husky group |

---

## 8. Test SDK (required, not optional)

| Package | Description |
|---|---|
| `Microsoft.NET.Test.Sdk` | Required for all test projects |
| `Microsoft.NETFramework.ReferenceAssemblies` | .NET Framework reference assemblies (only if `net48` TFM selected) |

---

## How versions are handled

This skill does **not** hardcode package version numbers -- they go stale too
fast. During bootstrap, the skill either:

1. Runs `dotnet package search <name> --take 1` to discover the latest stable
   version, or
2. Asks you to provide versions if you want specific ones, or
3. Adds packages with `dotnet add package <name>` which resolves the latest
   and writes the version into `Directory.Packages.props` automatically.

After initial setup, run `dotnet outdated` periodically to stay current.
