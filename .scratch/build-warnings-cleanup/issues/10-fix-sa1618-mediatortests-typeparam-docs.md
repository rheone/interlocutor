# 10 - Fix SA1618 - add typeparam docs in MediatorTests.cs

**What to build:** `tests/Interlocutor.Abstractions.Tests/MediatorTests.cs` declares six generic test-helper types - `MarkerConstrainedTrackingBehavior<TRequest, TResponse>`, `EjectingBeforeNextBehavior<TRequest, TResponse>`, `ThrowingBehavior<TRequest, TResponse>`, `EjectingAfterNextBehavior<TRequest, TResponse>`, `KeptTrackingBehavior<TRequest, TResponse>`, `DroppedTrackingBehavior<TRequest, TResponse>` - each with an XML `<summary>` but no `<typeparam>` tags for its two generic parameters, producing 12 `SA1618` warnings. Add real, descriptive `<typeparam>` docs to each (per the `csharp-docs-and-comments` skill's bar), not filler text.

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] `dotnet build` reports zero `SA1618` warnings for `tests/Interlocutor.Abstractions.Tests`
- [ ] Each added `<typeparam>` tag describes what that type parameter represents in the test double, not boilerplate
- [ ] `dotnet test tests/Interlocutor.Abstractions.Tests/Interlocutor.Abstractions.Tests.csproj` still passes
