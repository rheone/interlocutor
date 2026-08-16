## Labels

ready-for-agent

## What to build

Add the actual `LICENSE` file (Apache License 2.0, full text) at the repository root. The repository currently has no LICENSE file anywhere, in any form - the README cannot truthfully claim a license until this exists.

Once this lands, the README's license line/badge (ticket 22) and any package metadata (`<PackageLicenseExpression>` or equivalent in `.csproj`/`Directory.Build.props`, if such metadata exists) should reference it - check `Directory.Build.props` for existing package-metadata properties and set the license expression there if the property group already exists; don't invent a new packaging convention if one isn't already in place.

## Acceptance criteria

- [ ] `LICENSE` file exists at repo root with the unmodified Apache License 2.0 text
- [ ] Copyright line filled in with the correct holder and year(s)
- [ ] If `Directory.Build.props` (or per-project `.csproj` files) already declares NuGet package metadata, the license expression is set to `Apache-2.0`; if no such metadata exists yet, this ticket does not invent it

## Blocked by

None - can start immediately
