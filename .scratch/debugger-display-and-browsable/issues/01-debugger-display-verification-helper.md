# 01 - Test helper: verify `DebuggerDisplay` expressions stay valid

**What to build:** A reusable test helper that catches a `[DebuggerDisplay]` format string referencing a member
that doesn't exist - the one class of bug this attribute is prone to, since its expression is a plain string
literal the compiler never checks. Given a `Type` decorated with `[DebuggerDisplay]`, the helper parses the
`{Member}` / `{Member,nq}` / `{Member.Nested}` references out of the format string and asserts each resolves to
a real, readable member (property, field, or parameterless method) on that type via reflection. A member renamed
after the attribute was written then fails a test immediately instead of silently breaking the debugger
experience for whoever next steps through that type - worth building even though it protects a single
developer's debugging session, since that's exactly the moment nobody has time to notice the display went blank.

Every other ticket in this effort (`02` through `06`) calls this helper rather than hand-verifying its own
`DebuggerDisplay` expressions, so build it first.

**Blocked by:** None - can start immediately.

**Status:** done

- [x] A reusable test helper exists (placed wherever this repo's test-support code for cross-project helpers
      already lives, or a new shared test-support location if none does), taking a `Type` and asserting every
      member referenced by its `DebuggerDisplayAttribute`'s format string resolves.
- [x] The helper correctly parses the `{Member}`, `{Member,nq}`, and `{Member.NestedMember}` forms
      `DebuggerDisplay` expressions use, and ignores literal text outside `{}` braces.
- [x] A type with no `DebuggerDisplayAttribute` at all is treated as trivially passing, not an error - later
      tickets can call this helper defensively on any type without needing to know in advance whether it carries
      the attribute.
- [x] The helper has its own test coverage: at least one case proving it passes for a correct expression, and one
      proving it fails for an expression referencing a nonexistent member.
- [x] The helper carries an XML doc comment explaining its purpose and how to call it, so a later ticket finds
      and reuses it rather than re-implementing the same reflection.
- [x] `dotnet build` and `dotnet test` pass for the whole solution.

## Comments

**2026-09-04** - Re-triaged while working the frontier: already implemented in commit `9b27d74` ("feat: add DebuggerDisplay/DebuggerBrowsable across generator, DI, and result-type models (tickets 01-06)"), an ancestor of current HEAD. `tests/Interlocutor.TestSupport/DebuggerDisplayVerifier.cs` (static `AssertValid(Type)`) parses `{Member}`/`{Member,nq}`/`{Member.Nested}` tokens via regex, ignores literal text outside braces, and passes trivially for a type with no `[DebuggerDisplay]`. Its own test coverage (`Interlocutor.TestSupport.Tests/DebuggerDisplayVerifierTests.cs`) has 10 facts covering passing and failing cases. Full solution build/test/csharpier all clean. No source changes were needed this session.
