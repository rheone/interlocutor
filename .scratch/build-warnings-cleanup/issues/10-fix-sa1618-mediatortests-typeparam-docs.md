# 10 - Fix SA1618 - add typeparam docs in MediatorTests.cs

**What to build:** `tests/Interlocutor.Abstractions.Tests/MediatorTests.cs` declares six generic test-helper types - `MarkerConstrainedTrackingBehavior<TRequest, TResponse>`, `EjectingBeforeNextBehavior<TRequest, TResponse>`, `ThrowingBehavior<TRequest, TResponse>`, `EjectingAfterNextBehavior<TRequest, TResponse>`, `KeptTrackingBehavior<TRequest, TResponse>`, `DroppedTrackingBehavior<TRequest, TResponse>` - each with an XML `<summary>` but no `<typeparam>` tags for its two generic parameters, producing 12 `SA1618` warnings. Add real, descriptive `<typeparam>` docs to each (per the `csharp-docs-and-comments` skill's bar), not filler text.

**Blocked by:** None - can start immediately.

**Status:** done

- [x] `dotnet build` reports zero `SA1618` warnings for `tests/Interlocutor.Abstractions.Tests`
- [x] Each added `<typeparam>` tag describes what that type parameter represents in the test double, not boilerplate
- [x] `dotnet test tests/Interlocutor.Abstractions.Tests/Interlocutor.Abstractions.Tests.csproj` still passes

## Comments

Already implemented in commit `56141a9`. Verified: `tests/Interlocutor.Abstractions.Tests/MediatorTests.cs` contains 12 `<typeparam>` tags (2 each for the six named generic test-helper behaviors), and a full solution rebuild reports zero `SA1618` warnings for that project. `Interlocutor.Abstractions.Tests` passes (187/187).
