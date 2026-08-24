# .NET CLI command reference

Commands used by this skill to scaffold, build, and verify .NET library repos.
Run commands from the repo root unless noted.

## Creating projects

```bash
# Create a solution (.slnx format -- modern, human-readable, no GUIDs)
dotnet new slnx -n {{LibraryName}}

# Create the library project
dotnet new classlib -n {{LibraryName}} -o src/{{LibraryName}} -f {{TargetFrameworks}}

# Create the test project (pick one)
dotnet new xunit -n {{LibraryName}}.Tests -o src/{{LibraryName}}.Tests
dotnet new nunit -n {{LibraryName}}.Tests -o src/{{LibraryName}}.Tests
dotnet new mstest -n {{LibraryName}}.Tests -o src/{{LibraryName}}.Tests

# Create benchmarks project (console app, add BenchmarkDotNet after)
dotnet new console -n {{LibraryName}}.Benchmarks -o src/{{LibraryName}}.Benchmarks
```

## Wiring projects together

```bash
# Add projects to the solution (.slnx works with dotnet sln commands since SDK 9)
dotnet sln {{LibraryName}}.slnx add \
  src/{{LibraryName}}/{{LibraryName}}.csproj \
  src/{{LibraryName}}.Tests/{{LibraryName}}.Tests.csproj

# Add a project reference (tests depend on the library)
dotnet add src/{{LibraryName}}.Tests/{{LibraryName}}.Tests.csproj \
  reference src/{{LibraryName}}/{{LibraryName}}.csproj
```

## Adding packages

```bash
# Add a NuGet package reference (version resolved from Directory.Packages.props)
dotnet add src/{{LibraryName}}/{{LibraryName}}.csproj package Microsoft.SourceLink.GitHub

# Add package for a specific project
dotnet add src/{{LibraryName}}.Benchmarks/{{LibraryName}}.Benchmarks.csproj \
  package BenchmarkDotNet

# Add test packages
dotnet add src/{{LibraryName}}.Tests/{{LibraryName}}.Tests.csproj \
  package coverlet.collector

# See references/PACKAGES.md for the full recommended package list
```

## Restore and lock files

```bash
# Restore with lock file generation (enforced by Directory.Build.props)
dotnet restore --use-lock-file src/

# Force re-evaluate after changing package versions
dotnet restore --force-evaluate src/
```

## Build and test

```bash
# Build (run from src/)
dotnet build src/

# Test (all target frameworks)
dotnet test src/

# Test a single target framework
dotnet test src/ --framework net10.0

# Test with a filter
dotnet test src/ --filter "FullyQualifiedName~SubnetTests"
```

## Pack

```bash
# Pack the library into a .nupkg for smoke testing
dotnet pack src/{{LibraryName}}/{{LibraryName}}.csproj \
  -c Release \
  -o smoketests/{{LibraryName}}.SmokeTests/feed/
```

## Solution file management

```bash
# Add a project to the solution
dotnet sln {{LibraryName}}.slnx add path/to/project.csproj

# Remove a project from the solution
dotnet sln {{LibraryName}}.slnx remove path/to/project.csproj

# List solution contents
dotnet sln {{LibraryName}}.slnx list

# Never hand-edit .slnx files -- use these CLI commands instead.
```

## Checking availability

```bash
# List installed SDKs (to pick the version for global.json)
dotnet --list-sdks

# List installed runtimes
dotnet --list-runtimes

# Show local tools
dotnet tool list

# Restore local tools (from .config/dotnet-tools.json)
dotnet tool restore

# Search for latest package version
dotnet package search <name> --take 1
```

## Formatting and linting

```bash
# Run all formatters (style + analyzers + csharpier -- order matters)
dotnet format style && dotnet format analyzers && dotnet csharpier format .

# Format only staged files (used by husky pre-commit)
dotnet format style --no-restore --include <staged-file-list>
```
