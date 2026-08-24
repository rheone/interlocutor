# 02 - Fix XML-doc and ordering warnings in Interlocutor.Abstractions and its tests

**What to build:** `Interlocutor.Abstractions` and `Interlocutor.Abstractions.Tests` currently build clean but with a cluster of StyleCop/compiler doc-comment and member-ordering warnings. Bring the package's public XML docs and member ordering in line with house style (see `csharp-docs-and-comments` and `csharp-code-organization` conventions):

- SA1609 (missing XML doc on a property) - 8 instances, `src/Interlocutor.Abstractions`
- SA1414 (tuple element names should be included in doc comment) - 3 instances, `src/Interlocutor.Abstractions`
- SA1214 (static readonly fields should appear before non-readonly fields) - 1 instance, `src/Interlocutor.Abstractions`
- SA1201 (elements should appear in the correct order) - 2 instances in `src/Interlocutor.Abstractions`, 9 in `tests/Interlocutor.Abstractions.Tests`
- CS1734 (XML comment has a `paramref`/`typeparamref` for a parameter that doesn't exist) - 4 instances, `src/Interlocutor.Abstractions`
- CS1574 (XML comment cref could not be resolved) - 4 instances in `src/Interlocutor.Abstractions`, 1 in `tests/Interlocutor.Abstractions.Tests`
- SA1618 (generic type parameter documentation missing) - 1 instance, `tests/Interlocutor.Abstractions.Tests`

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] `dotnet build` reports zero SA1609, SA1414, SA1214, SA1201, CS1734, CS1574, and SA1618 warnings for `Interlocutor.Abstractions` and `Interlocutor.Abstractions.Tests`
- [ ] Fixed `cref`/`paramref`/`typeparamref` targets actually resolve to the real member/parameter/type-parameter names (not just silenced)
- [ ] Member ordering changes don't alter any public API shape - only reordering and doc-comment content change
- [ ] `dotnet test tests/Interlocutor.Abstractions.Tests/Interlocutor.Abstractions.Tests.csproj` still passes
