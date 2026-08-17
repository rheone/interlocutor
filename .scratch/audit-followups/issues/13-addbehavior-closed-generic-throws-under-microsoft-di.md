## Labels

ready-for-agent

## What to build

`GeneratedRegistration_WithMultipleBehaviors_ExecutesBehaviorsInCorrectOrder` in `tests/Interlocutor.DependencyInjection.Microsoft.Tests/GeneratedRegistrationIntegrationTests.cs` currently **fails**, discovered incidentally while re-verifying the remediation effort's defects (unrelated to that effort - a real, separate regression).

`builder.AddBehavior(typeof(ClosedGenericBehaviorType))` throws `ArgumentException: Open generic service type ... requires registering an open generic implementation type` when the type passed to `AddBehavior` is a *closed* (non-open-generic) custom behavior implementation, registered under Microsoft.Extensions.DependencyInjection. `AddBehavior` always registers against the open `IPipelineBehavior<,>` service type regardless of whether the implementation type passed in is itself open or closed generic (per `InterlocutorBuilder.AddBehavior`'s own documented shape) - Microsoft DI's open-generic resolver rejects that combination when the implementation side isn't also an open generic definition. The Autofac translator branches on `ImplementationType.IsGenericTypeDefinition` (`RegisterGeneric` vs `RegisterType`) specifically to avoid this; the Microsoft DI translator does not have an equivalent branch.

## Acceptance criteria

- [ ] `builder.AddBehavior(typeof(SomeClosedGenericBehavior))` (a behavior implementation that is itself closed/non-generic, or generic but not an open generic type definition) registers and resolves successfully under `Interlocutor.DependencyInjection.Microsoft`, mirroring how the Autofac translator already handles this distinction
- [ ] `GeneratedRegistration_WithMultipleBehaviors_ExecutesBehaviorsInCorrectOrder` passes
- [ ] A regression test covers both shapes explicitly: an open-generic behavior implementation (existing, already-passing case) and a closed behavior implementation, both registered via `AddBehavior` under Microsoft DI
- [ ] `dotnet test tests/Interlocutor.DependencyInjection.Microsoft.Tests/Interlocutor.DependencyInjection.Microsoft.Tests.csproj` passes in full

## Blocked by

None - can start immediately
