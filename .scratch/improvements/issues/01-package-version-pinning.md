> **Status: superseded** - `Directory.Packages.props` now pins every `PackageVersion` to an exact version (no `4.*`/`1.*`/`11.0.0-*` floats remain), and `CentralPackageFloatingVersionsEnabled` is unset. Builds are already deterministic; no lock file was needed. No action needed here; historical context only.

## Labels

ready-for-agent

## What to build

Pin all NuGet package versions in `Directory.Packages.props` or enable `RestorePackagesWithLockFile` to eliminate build-from-source reproducibility risk.

Currently, all package versions use floating patterns (`4.*`, `1.*`, `11.0.0-*`). Combined with `CentralPackageFloatingVersionsEnabled=true` and no lock file, the same commit can build with different dependency versions on different machines or at different times.

Option A - pin to specific versions in `Directory.Packages.props`:
  - Resolve current floating versions to specific numbers
  - Update each `Version` attribute to the exact version
  - Disable `CentralPackageFloatingVersionsEnabled`

Option B - enable lock file:
  - Set `RestorePackagesWithLockFile=true` in `Directory.Build.props`
  - Run `dotnet restore` to generate `packages.lock.json`
  - Check the lock file in

Either approach suffices. The key outcome is deterministic builds.

## Acceptance criteria

- [ ] Running `dotnet restore && dotnet build` on a fresh clone produces identical package versions every time
- [ ] CI or pre-commit detects a version drift if `packages.lock.json` is stale (if Option B)
- [ ] All existing tests still pass

## Blocked by

None - can start immediately
