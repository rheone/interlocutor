# 17 - Fix NuGet packaging metadata

**What to build:** Every packable project's NuGet metadata is publish-ready: no literal `TBD` placeholder, and a package README/icon configured so the package looks intentional on nuget.org/in an IDE's package browser.

Open decision for whoever picks this up: what the real `Company` value should be (or whether to drop the property, since `Authors` already carries attribution) — this is a business fact the review can't determine.

Full technical detail: [findings/17-nuget-packaging-metadata-placeholder.md](../findings/17-nuget-packaging-metadata-placeholder.md).

**Blocked by:** None - can start immediately.

**Status:** done

- [x] `Directory.Build.props`'s `Company` is set to a real value, or removed
- [x] `PackageReadmeFile` is set (README.md is a suitable candidate) across packable projects via `Directory.Build.props`
- [x] `PackageIcon` is configured, or a decision is recorded that one isn't needed yet

## Comments

**2026-09-04** - Re-triaged while working the frontier: already implemented in commit `6a3f9ce`. Directory.Build.props drops the placeholder Company value (with a comment explaining why - individual-authored, Authors already carries attribution) and sets PackageReadmeFile to README.md across packable projects; PackageIcon is deliberately left unset with a recorded comment rather than a fabricated placeholder. No source changes were needed this session.
