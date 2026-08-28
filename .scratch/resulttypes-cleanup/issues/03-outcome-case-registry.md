> **Status: done** - merged in `a5d96ca`.

# 03 - IOutcomeCaseRegistry extension-point

## Labels

ready-for-agent

**What to build:** Today, `AuthorizationBehavior` constructs its `Forbidden` short-circuit case via `UnionCaseFactory`, reflection that only works against compiler-generated `union` types (a constructor accepting exactly one case-type argument). A library user who implements their own `IOperationOutcome` (not a compiler `union`) can't be short-circuited this way, and the ASP.NET Core mapping helpers (ticket 14) need the same capability to extract the active case from an arbitrary `IOperationOutcome`. Build a registry - mirroring the existing `OutcomeClassifierBuilder`/`IOutcomeClassifier` pattern in `Interlocutor.Abstractions.Pipeline.Classifiers` - that can construct a `TResponse` from a given case value, and extract the active case type/value from an existing `TResponse`, for any `IOperationOutcome` implementation. Pre-seed it with the existing reflection-based union-constructor lookup as the zero-config default; let consumers register a custom factory/extractor for their own outcome type. Fail-closed: an unsupported case throws a clear, named exception at first use.

**Blocked by:** None - can start immediately

- [x] `Interlocutor.Abstractions.Pipeline.Cases.IOutcomeCaseRegistry` interface exists with a `Construct<TResponse, TCase>(TCase)` method and a `TryGetActiveCase<TResponse>(TResponse, out Type, out object?)` method
- [x] `DefaultOutcomeCaseRegistry` implements it by wrapping the existing `UnionCaseFactory` reflection (constructor lookup cached per `(Type, Type)` pair) and pattern-matching the compiler-generated `IUnion` marker for extraction
- [x] `OutcomeCaseRegistryBuilder` lets a consumer register a custom `Func<TCase, TResponse>` factory and a custom extractor for their own `IOperationOutcome` type, falling back to `DefaultOutcomeCaseRegistry` behavior for anything unregistered
- [x] `OutcomeCaseNotSupportedException` is thrown (not silently swallowed) when `Construct`/`TryGetActiveCase` can't resolve a case for a given `TResponse`, with a message in the same style as `AuthorizationBehavior`'s existing exception text
- [x] Unit tests cover `Construct`/`TryGetActiveCase` against a real `OperationOutcome<...>` union (both a case that exists and one that doesn't) AND against a hand-rolled, non-union, test-only `IOperationOutcome` implementation with a registered factory/extractor pair
- [x] No reference to `Interlocutor.AspNetCore` or any store-specific package from this new code (verify `Interlocutor.Architecture.Tests` still passes)
- [x] `dotnet test tests/Interlocutor.Abstractions.Tests/Interlocutor.Abstractions.Tests.csproj` and `tests/Interlocutor.Architecture.Tests/Interlocutor.Architecture.Tests.csproj` pass
