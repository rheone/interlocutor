# 06 - Revisit ResultExtensions.cs member order once StyleCop.Analyzers supports C# 14 extension blocks

**What to build:** `StyleCop.Analyzers` 1.2.0-beta.556 (pinned in `Directory.Packages.props`) doesn't
classify C# 14 extension block declarations for its SA1201 member-ordering check, so it misreports a
location-less `warning SA1201: A <unknown> should not follow a field/method` wherever an extension block
sits immediately after a field or method. `src/Interlocutor.AspNetCore/ResultExtensions.cs` (ticket 05 of
this effort) works around it by ordering the type public-surface-first - every `extension` block grouped
together, in arity order - followed by the private fields and private static helpers together at the
bottom, rather than the usual fields-first canonical order. That produces zero warnings without disabling
SA1201 (no `.editorconfig` override, no `#pragma` - both were tried and rejected: a pragma can't reach a
diagnostic with no source location, and a file-scoped severity override would have hidden any *genuine*
future ordering mistake in the file, not just this false positive), but it's a deliberate accommodation of
a tooling gap, not the order the file would take on its own merits.

Once a `StyleCop.Analyzers` release classifies extension block declarations correctly for SA1201, upgrade
the pinned version (`Directory.Packages.props`) and re-evaluate whether `ResultExtensions.cs`'s member order
should move back to interleaving each extension block with the private helper(s) most relevant to it, now
that the analyzer can enforce correct ordering on its own. Also sweep the rest of the codebase for any other
file that hit the same adjacency (this effort's tickets 02-04 didn't, because their extension blocks were
each a file's only member, but a future extension-block conversion elsewhere might) and remove any similar
accommodation there too.

**Blocked by:** None to file - but the actual work is gated on an external `StyleCop.Analyzers` release
that doesn't exist yet. Not "ready-for-agent" in the pull-and-work sense; revisit next time
`StyleCop.Analyzers` is upgraded (see `build-warnings-cleanup` or a similar future dependency-bump ticket).

**Status:** needs-triage

- [ ] `StyleCop.Analyzers` has been upgraded (in `Directory.Packages.props`) past 1.2.0-beta.556 to a version
      that classifies C# 14 extension block declarations for SA1201
- [ ] `dotnet build` on `Interlocutor.AspNetCore` reports zero SA1201 warnings with `ResultExtensions.cs`
      returned to its natural member order (or a documented decision that the public-surface-first order is
      kept anyway, on its own merits, once it's no longer a tooling workaround)
- [ ] The codebase has been swept for any other file carrying the same ordering accommodation, and each is
      resolved the same way
- [ ] `dotnet test` passes solution-wide
