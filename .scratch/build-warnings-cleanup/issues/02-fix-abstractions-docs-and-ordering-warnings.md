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

**Status:** done

- [x] `dotnet build` reports zero SA1609, SA1414, SA1214, SA1201, CS1734, CS1574, and SA1618 warnings for `Interlocutor.Abstractions` and `Interlocutor.Abstractions.Tests`
- [x] Fixed `cref`/`paramref`/`typeparamref` targets actually resolve to the real member/parameter/type-parameter names (not just silenced)
- [x] Member ordering changes don't alter any public API shape - only reordering and doc-comment content change
- [x] `dotnet test tests/Interlocutor.Abstractions.Tests/Interlocutor.Abstractions.Tests.csproj` still passes (153/153)

## Comments

- SA1609: added `<value>` tags to the flagged properties (`HandlerNotRegisteredException`, `UnsupportedRequestTypeException`, `NotificationHandlerRunAfterAttribute`, `NotificationHandlerRunBeforeAttribute`, `HandlerOverrideAttribute`, `OutcomeCaseNotSupportedException`).
- SA1414: named the tuple elements in `OutcomeClassifierBuilder`'s internal `DefaultOutcomeClassifier` constructor signature (`Predicate`, `Label`, `IsFailure`), matching the already-named tuple used everywhere else in the same type.
- SA1214/SA1201: reordered `OutcomeClassifierBuilder` (readonly field before the non-readonly field; constructor before the property) and `DefaultOutcomeCaseRegistry` (field before property) to match StyleCop's canonical member order. In the test project, moved nested test-fixture records/classes to the bottom of their class (StyleCop orders class/record/struct nested types after methods) and one nested interface to the top (interfaces order before methods) in `LoggingBehaviorTests`, `AuditingBehaviorTests`, `AuthorizationBehaviorTests`, `CancellationBehaviorTests`, `RateLimitedBehaviorTests`, `TimeoutBehaviorTests`, `DelegatePipelineStageTests`, `OutcomeClassifierBuilderTests`, `NotificationFanOutTests` (top-level types), `AuditingBehaviorAspNetCoreIntegrationTests`, `RegistrationReportTests`, `ContainerBuilderExtensionsTests`, `GeneratedRegistrationIntegrationTests` (the last four are covered by ticket 04's SA1201 list but were the same underlying rule).
- CS1734: `TimeoutBehavior`'s class-level `<remarks>` used `<paramref name="ct">` outside any member that declares `ct` - changed to `<c>ct</c>`.
- CS1574: `Mediator.cs`'s doc referenced a since-renamed `DispatchViaReflectionAsync` (now `DispatchAsync`); `OutcomeCaseRegistryBuilder.cs` and `DefaultResponseOutcomeDescriber.cs` referenced the compiler-synthesized `IUnion` without the `System.Runtime.CompilerServices` qualification other files in the same namespace already use; `UnionCaseFactory.cs` referenced `Interlocutor.FluentValidation.ValidationBehavior{TRequest,TResponse}`, a type in a package `Interlocutor.Abstractions` architecturally cannot reference - changed to a `<c>` code span instead of a `cref`.
- SA1618: added a `<typeparam name="T">` to `MediatorActivityTracingTests`'s `RecordingLogger<T>`.
