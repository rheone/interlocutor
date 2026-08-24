## Labels

ready-for-agent

## What to build

`GeneratedRegistration_WithMultipleBehaviors_ExecutesBehaviorsInCorrectOrder` in `tests/Interlocutor.DependencyInjection.Microsoft.Tests/GeneratedRegistrationIntegrationTests.cs` currently **fails** - reproduced directly (not just reported secondhand):

```
System.ArgumentException : Open generic service type 'Interlocutor.Abstractions.Pipeline.IPipelineBehavior`2[TRequest,TResponse]' requires registering an open generic implementation type. (Parameter 'descriptors')
   at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteFactory.Populate()
   at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteFactory..ctor(ICollection`1 descriptors)
   at Microsoft.Extensions.DependencyInjection.ServiceProvider..ctor(ICollection`1 serviceDescriptors, ServiceProviderOptions options)
   ...
   at Interlocutor.DependencyInjection.Microsoft.Tests.GeneratedRegistrationIntegrationTests.GeneratedRegistration_WithMultipleBehaviors_ExecutesBehaviorsInCorrectOrder() in .../GeneratedRegistrationIntegrationTests.cs:line 63
```

Discovered incidentally while re-verifying the remediation effort's defects; unrelated to that effort, a real, separate regression. Root cause pinned down by reading both sides directly:

- `InterlocutorBuilder.AddBehavior` (`src/Interlocutor.DependencyInjection.Abstractions/InterlocutorBuilder.cs:215-229`) always pairs whatever implementation type it's given with the **open** `typeof(IPipelineBehavior<,>)` as `ServiceType`, regardless of whether the implementation type itself is open or closed generic.
- `ServiceCollectionExtensions.Apply` (`src/Interlocutor.DependencyInjection.Microsoft/ServiceCollectionExtensions.cs:95-112`) translates every registration with a single, unconditional `services.AddScoped/AddSingleton/AddTransient(registration.ServiceType, registration.ImplementationType)` call - no branch on whether `ImplementationType` is itself an open generic definition.
- The failing test's behaviors (`OrderTrackingBehavior1/2/3`, same file, ~line 76) each implement a **closed** `IPipelineBehavior<GeneratedQuery, OperationOutcome<string, Failure>>` directly - a legitimate, documented shape (`AddBehavior`'s own remarks describe accepting "an open generic type definition... or a closed type"). Pairing the open `IPipelineBehavior<,>` service type with a closed implementation type is not a legal Microsoft DI registration shape; the container throws at `BuildServiceProvider()` time (lazily - not at the `AddScoped` call site, which is why it doesn't surface until the test actually builds its provider).
- `ContainerBuilderExtensions.cs` (Autofac, `src/Interlocutor.DependencyInjection.Autofac/`) already handles exactly this distinction correctly: `if (registration.ImplementationType.IsGenericTypeDefinition) builder.RegisterGeneric(...) else builder.RegisterType(...)`. The Microsoft DI translator has no equivalent branch - this is the fix pattern already proven in this same repo, just missing on one side.

## Acceptance criteria

- [x] `ServiceCollectionExtensions.Apply` (or wherever the fix naturally lands) registers a closed-generic behavior implementation against the specific closed `IPipelineBehavior<TRequest, TResponse>` it actually implements, not against the open `IPipelineBehavior<,>` - mirroring the open/closed branch the Autofac translator already has
- [x] An open-generic behavior implementation (e.g. `LoggingBehavior<,>`) continues to register and resolve exactly as it does today - no regression to the already-working path
- [x] `GeneratedRegistration_WithMultipleBehaviors_ExecutesBehaviorsInCorrectOrder` passes
- [x] A regression test covers both shapes explicitly under Microsoft DI: an open-generic behavior implementation and a closed one, both registered via `AddBehavior`, both resolving and executing
- [x] `dotnet test tests/Interlocutor.DependencyInjection.Microsoft.Tests/Interlocutor.DependencyInjection.Microsoft.Tests.csproj` passes in full (2/2, currently 1/2)

## Blocked by

None - can start immediately
