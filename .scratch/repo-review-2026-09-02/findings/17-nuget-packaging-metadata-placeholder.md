Status: needs-triage
Severity: LOW
Category: other (packaging/release-readiness)
Source: antagonist lane's independent gap-hunt (no lane examined packaging metadata)

# NuGet package metadata ships a literal placeholder and no README/icon, solution-wide

## Summary

Every packable project inherits `Company = "TBD"` from the shared `Directory.Build.props`, and none configure a package README or icon, so a `dotnet pack`/publish today would ship that literal placeholder text as the `Company` metadata on nuget.org.

## Evidence

`Directory.Build.props:12`: `<Company>TBD</Company>`. No `PackageReadmeFile`/`PackageIcon` property anywhere in `Directory.Build.props` or any packable `src/*.csproj` (e.g. `Interlocutor.CommonKernel.csproj` sets `IsPackable=true`). `Directory.Build.props` already centralizes `Authors`, `Copyright`, `PackageLicenseExpression`, `RepositoryUrl`, etc. — a reasonable, DRY setup — but `Company` was evidently left as a placeholder. `README.md` exists and is a genuinely good candidate package readme but isn't wired up via `PackageReadmeFile`.

## Suggested action

Either set `Company` to the real value (or delete the property if there's no company entity, since `Authors` already carries attribution) and add `PackageReadmeFile`/`PackageIcon` before any real publish, or — if publishing to a public feed isn't imminent — leave a tracking note so this doesn't ship by accident on the first `dotnet nuget push`.
