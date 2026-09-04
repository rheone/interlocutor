# 02 - `RegistrationReport` and DI-registration debugging

**What to build:** A developer debugging "why isn't X registered" gets a useful glance at `RegistrationReport` in
the debugger - a count-based summary (registrations, problems, skipped entries, type-load failures) - instead of
a bare type name, since it currently has no `ToString()` override at all. Each of the list-entry types
`RegistrationReport` exposes (its registration entries, skipped-registration entries, validation-problem entries,
and type-load-failure entries) also gets a useful display if it doesn't already have one, so expanding one of
those lists in the debugger is informative on its own, without a further expand per entry.

**Blocked by:** Ticket 01.

**Status:** done

- [x] `RegistrationReport` carries a `[DebuggerDisplay]` summarizing the counts of `Registrations`, `Problems`,
      `Skipped`, and `TypeLoadFailures`.
- [x] Every list-entry type `RegistrationReport` exposes is reviewed; any that doesn't already have a useful
      default display (a hand-written or record-synthesized `ToString()` that shows its identifying fields, not
      just a type name) gains a `[DebuggerDisplay]` surfacing its kind/name/reason.
- [x] Every `[DebuggerDisplay]` added in this ticket is verified via Ticket 01's helper.
- [x] No public API, behavior, or equality change - this ticket is debug-view-only.
- [x] `dotnet build` and `dotnet test` pass for the whole solution.

## Comments

**2026-09-04** - Re-triaged while working the frontier: already implemented in commit `9b27d74`. `RegistrationReport` carries a count-based `[DebuggerDisplay]`; `InterlocutorRegistration` and `InterlocutorFactoryRegistration` (its list-entry types) each carry their own `[DebuggerDisplay]` showing kind/service/implementation/lifetime. Verified via `Interlocutor.TestSupport.Tests`'s `DebuggerDisplayVerifier` in `tests/Interlocutor.DependencyInjection.Abstractions.Tests/DebuggerDisplayTests.cs`. Debug-view-only, no public API change. No source changes were needed this session.
