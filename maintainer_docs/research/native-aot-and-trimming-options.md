# Native AOT and Trimming: What Actually Blocks Us, and What It Would Cost to Fix

**Status:** research findings, no decision taken. Written against the working tree of the `improvements` branch (the in-progress `IsAotCompatible`/`PublishAot` annotation work), SDK `11.0.100-preview.6.26359.118`.

This paper exists to test a standing claim. [ADR-0008](../adr/0008-compile-time-handler-discovery.md) asserts that compile-time handler discovery "enables build-time diagnostics and AOT/trim compatibility," and its Consequences section states as a positive that "the library becomes AOT- and trim-compatible, a real differentiator against reflection-based mediator libraries." Half of that claim is true today and half of it is not.

> [!CAUTION]
> **Three premises this research was commissioned on turned out to be wrong.** They are corrected in [Corrections to the stated premises](#corrections-to-the-stated-premises) and each is called out again where it matters. If you read nothing else, read that section - two of the three change what the fix has to be, and one of them changes what CI can gate.

---

## 1. The constraint, precisely

### 1.1 What actually throws

Running the sample as an ordinary framework-dependent app - `dotnet bin/Release/net11.0/Interlocutor.Sample.WebApi.dll`, no native compilation involved - every dispatch fails:

```
System.InvalidOperationException: Unable to create a generic service for type
'Interlocutor.Abstractions.Pipeline.IPipelineBehavior`2[GetTodoByIdQuery, OperationOutcome`2[TodoItem, NotFound]]'
because 'OperationOutcome`2[TodoItem, NotFound]' is a ValueType.
Native code to support creating generic services might not be available with native AOT.
   at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteFactory.VerifyOpenGenericAotCompatibility(...)
```

*Empirically reproduced in this repo: `dotnet build -c Release`, then `dotnet bin/Release/net11.0/Interlocutor.Sample.WebApi.dll` with `curl http://127.0.0.1:PORT/todos/1`.*

### 1.2 The guard in dotnet/runtime

`CallSiteFactory.CreateOpenGeneric` runs the check before it constructs the closed implementation type ([dotnet/runtime `src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceLookup/CallSiteFactory.cs:546-551`](https://github.com/dotnet/runtime/blob/258621c819334857be3be891228429bb31c6eab8/src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceLookup/CallSiteFactory.cs#L546-L551)):

```csharp
Type[] genericTypeArguments = serviceIdentifier.ServiceType.GenericTypeArguments;
if (!ServiceProvider.IsDynamicCodeSupported)
{
    VerifyOpenGenericAotCompatibility(serviceIdentifier.ServiceType, genericTypeArguments);
}

closedType = implementationType.MakeGenericType(genericTypeArguments);
```

and the guard itself ([same file, `:828-837`](https://github.com/dotnet/runtime/blob/258621c819334857be3be891228429bb31c6eab8/src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceLookup/CallSiteFactory.cs#L828-L837)):

```csharp
private static void VerifyOpenGenericAotCompatibility(Type serviceType, Type[] genericTypeArguments)
{
    foreach (Type typeArg in genericTypeArguments)
    {
        if (typeArg.IsValueType)
        {
            throw new InvalidOperationException(SR.Format(SR.AotCannotCreateGenericValueType, serviceType, typeArg));
        }
    }
}
```

Its doc comment states the intent plainly: "NativeAOT apps are not guaranteed that the native code for the closed generic of ValueType has been generated. To catch these problems early, this verification is enabled at development-time to inform the developer early that this scenario will not work once AOT'd."

`ServiceProvider.IsDynamicCodeSupported` is a straight forward of the runtime feature ([`ServiceProvider.cs:45-50`](https://github.com/dotnet/runtime/blob/258621c819334857be3be891228429bb31c6eab8/src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceProvider.cs#L45-L50)):

```csharp
internal static bool IsDynamicCodeSupported =>
#if NETFRAMEWORK || NETSTANDARD2_0
    true;
#else
    RuntimeFeature.IsDynamicCodeSupported;
#endif
```

A second, separate guard fires when the *enumerated item type itself* is a value type ([`CallSiteFactory.cs:275-280`](https://github.com/dotnet/runtime/blob/258621c819334857be3be891228429bb31c6eab8/src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceLookup/CallSiteFactory.cs#L275-L280)). That one does not apply to us - `IPipelineBehavior<,>` is an interface. It *would* apply to anyone doing `GetServices<OperationOutcome<...>>()`, which nothing in this library does.

### 1.3 Which feature switch drives it

> [!IMPORTANT]
> **`VerifyOpenGenericServiceTrimmability` has nothing to do with this throw.** The guard keys exclusively off `System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported`. This contradicts the premise this research was commissioned on.

Four runs of the sample, toggling the two switches independently in `Interlocutor.Sample.WebApi.runtimeconfig.json` and hitting `GET /todos/1` (*empirically verified in this repo*; 404 means the pipeline resolved and the handler returned `NotFound` against an empty in-memory database, i.e. success):

| `VerifyOpenGenericServiceTrimmability` | `RuntimeFeature.IsDynamicCodeSupported` | Result |
| --- | --- | --- |
| `true` | `false` | **500** - `VerifyOpenGenericAotCompatibility` throws |
| `false` | `false` | **500** - identical throw |
| `true` | `true` | 404 - resolves fine |
| *(absent)* | *(absent)* | 404 - resolves fine |

`VerifyOpenGenericServiceTrimmability` is a *different* check, at [`CallSiteFactory.cs:65`](https://github.com/dotnet/runtime/blob/258621c819334857be3be891228429bb31c6eab8/src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceLookup/CallSiteFactory.cs#L65), validating trimming annotations on open-generic registrations. It emits no error for our shape.

### 1.4 Is it a conservative guard, or a real limitation?

It is a conservative guard **in front of a real limitation**. The .NET docs state the underlying rule directly ([Native AOT deployment overview - Limitations](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/#limitations-of-native-aot-deployment)):

> Generic parameters substituted with struct type arguments have specialized code generated for each instantiation. In the dynamic runtime, many instantiations are generated on-demand. In Native AOT, all instantiations are pre-generated.

Reference-type instantiations share one canonical body; value-type instantiations do not. With no JIT, an instantiation the compiler never saw simply does not exist. Section 2 shows a container *without* the guard failing on the same shape, later and with a worse error message - so the guard is genuinely protective, not merely pessimistic.

### 1.5 Does a closed registration bypass the guard?

Yes, completely. The guard lives inside `CreateOpenGeneric`, which is only reached for a descriptor whose `ServiceType` is an open generic type definition. A pre-closed `ServiceDescriptor` never enters that path. Section 3 proves this both under the feature switches and under a genuine native binary.

> [!WARNING]
> The bypass is not additive. `TryCreateEnumerable` walks **every** descriptor matching the service type, so leaving one open-generic `IPipelineBehavior<,>` descriptor in the collection alongside the closed ones re-triggers the throw. Closed registration must *replace* the open-generic registration, not supplement it. *Empirically verified in this repo (scratch probe, "CLOSED + leftover open generic" case).*

---

## 2. Other containers hit the same wall - the guard is not the cause

To answer this honestly the test has to be a genuinely native-compiled binary, not `dotnet run` with feature switches flipped. It is - see [§7.1](#71-the-dev-container-can-already-do-a-real-native-publish) for why that was possible here.

A probe referencing `Interlocutor.Abstractions` plus Autofac 9.3.2, published with `dotnet publish -c Release -r linux-x64` and `PublishAot=true`, then executed as the resulting ELF binary (*empirically verified in this repo*):

| Case | Native AOT | Trimmed (`PublishTrimmed`, JIT present) |
| --- | --- | --- |
| **Microsoft DI**, open generic, struct response | **throws** `InvalidOperationException` (the guard) | passes |
| **Microsoft DI**, closed registration, struct response | **passes** | passes |
| **Autofac** `RegisterGeneric`, struct response, instantiation *also present statically elsewhere* | **passes** | passes |
| **Autofac** `RegisterGeneric`, struct response, instantiation present *nowhere* statically | **throws** `NotSupportedException: … is missing native code or metadata` | passes |
| **Autofac** `RegisterType` closed, struct response | **passes** | passes |
| **Autofac** `RegisterGeneric` with marker-interface constraint filtering | **throws** `TypeLoadException: Could not resolve type 'ITransactionalRequest'` | passes |

Two conclusions, both load-bearing:

1. **The wall is the runtime's, not Microsoft DI's.** Autofac has no guard, so it gets further before failing - and then fails with a message that points at the wrong thing. Microsoft DI's guard is strictly better developer experience for the same underlying impossibility.
2. **What decides success is whether the exact closed instantiation was statically visible to the compiler**, not which container asked for it. Autofac's `RegisterGeneric` "passed" in row three only because the identical instantiation had been written out in source in an earlier test case, so ILCompiler had already generated it. That is the mechanism [§3](#3-closed-registrations-the-fix-and-what-it-has-to-cover) depends on, demonstrated by accident.

### 2.1 Autofac documents this

Autofac annotates `RegisterGeneric` itself ([autofac/Autofac `src/Autofac/RegistrationExtensions.Generics.cs:17,26`](https://github.com/autofac/Autofac/blob/be20421d884a0a2d2389a4e4d2c2e00eff65f960/src/Autofac/RegistrationExtensions.Generics.cs#L17-L26)):

```csharp
private const string OpenGenericDynamicCodeWarning = "Registering open generic types constructs closed generic types at runtime via MakeGenericType, which may require dynamic code generation when closed over value types and is not compatible with native AOT.";

[RequiresDynamicCode(OpenGenericDynamicCodeWarning)]
public static IRegistrationBuilder<object, ReflectionActivatorData, DynamicRegistrationStyle>
    RegisterGeneric(this ContainerBuilder builder, [DynamicallyAccessedMembers(ActivatorMemberTypes.ActivatedType)] Type implementer)
```

That warning text names our exact scenario. It is also raised as an IL3050 in this repo today - and suppressed. See [§8.2](#82-the-suppressions-currently-in-src-are-not-true).

The constraint-filtering row is a distinct Autofac-under-AOT problem: the trimmer removed `ITransactionalRequest`'s type metadata, which Autofac needs at resolve time to evaluate the `where` clause. Compile-time constraint evaluation (what a generator does) does not need that metadata at all.

### 2.2 Lamar, DryIoc, SimpleInjector

> [!NOTE]
> **Evidence here is thin and I could not test these empirically.** What I could verify from primary sources:
>
> - **Lamar** ([JasperFx/lamar `src/Lamar/Lamar.csproj`](https://github.com/JasperFx/lamar/blob/master/src/Lamar/Lamar.csproj)) sets no `IsAotCompatible`/`IsTrimmable`, and depends on the JasperFx runtime-Roslyn-compilation stack. Native AOT forbids runtime code generation outright ([docs, Limitations](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/#limitations-of-native-aot-deployment)), so Lamar is out of scope for AOT regardless of what this library does.
> - **DryIoc** ([dadhi/DryIoc `src/DryIoc/DryIoc.csproj`](https://github.com/dadhi/DryIoc/blob/master/src/DryIoc/DryIoc.csproj)) sets no AOT/trim properties and its README makes no AOT claim.
> - **SimpleInjector** - no AOT/trim statement found in its README.
>
> None of these publish an AOT-support claim I could cite. The section 2 finding still applies to all of them: a value-type generic instantiation the compiler never saw cannot be created at runtime, whoever asks for it. Any container that closes an open generic at resolve time will fail the same way. **Do not treat "we support container X under AOT" as verified for anything other than Microsoft DI and Autofac.**

---

## 3. Closed registrations: the fix, and what it has to cover

This is the decision-relevant question, so it got the most effort - including an end-to-end experiment in the real sample app.

### 3.1 Microsoft DI, native binary, closed registration: works

The native-AOT probe row "**Microsoft DI**, closed registration, struct response → passes" is the direct answer to (a) and (b) together. The registration

```csharp
services.AddScoped<
    IPipelineBehavior<PlainRequest, OperationOutcome<Widget, NotFound>>,
    Noop<PlainRequest, OperationOutcome<Widget, NotFound>>>();
```

both avoids `CreateOpenGeneric` entirely and gives ILCompiler the instantiation in generic-argument position, which is enough for it to pre-generate the code. Resolution succeeded in a stripped, JIT-free ELF binary.

### 3.2 End-to-end in the sample app

The sample's `Program.cs` was temporarily patched to strip all three open-generic `IPipelineBehavior<,>` descriptors and hand-write closed replacements per concrete `(TRequest, TResponse)` pair, then run under the AOT feature switches (*empirically verified in this repo; the patch has been reverted*):

```
[EXPERIMENT] removing 3 open-generic behavior descriptors
GET   /todos/1 : 500
```

but the 500 is now a **different exception entirely**:

```
System.InvalidOperationException: Query wasn't precompiled and dynamic code isn't supported with NativeAOT.
   at Microsoft.EntityFrameworkCore.Query.Internal.QueryCompiler...
   at Interlocutor.Sample.Handlers.Todos.GetTodoByIdHandler.Handle(GetTodoByIdQuery query, ...)
   at Interlocutor.Abstractions.Pipeline.PipelineStageBehavior`2.Handle(...)
   at Interlocutor.Abstractions.Pipeline.LoggingBehavior`2.Handle(...)
```

Read the stack bottom-up: `LoggingBehavior` wrapped `PipelineStageBehavior` wrapped the handler, in exactly the onion order [ADR-0002](../adr/0002-pipeline-behavior-onion-model.md) specifies. The DI guard never fired. The library's part of the pipeline worked; what failed is EF Core's own unrelated AOT gap (uncompiled LINQ), which is a sample concern, not a library one.

**Closed registration is a complete fix for the DI half of the problem.** It is not a complete fix for the whole problem - see [§3.5](#35-the-second-blocker-nobody-mentioned-the-mediators-own-wrapper-construction).

### 3.3 Ordering and duplicates

`GetServices<T>()` over closed descriptors returns them in registration order, and duplicates are preserved rather than collapsed - verified with two behaviors registered against the same closed service type, resolved as `(Noop, TxOnly)` in that order under both JIT and native AOT. `InterlocutorPipelineBuilder`'s documented "flat list in builder-call order, duplicates are legal, nothing de-duplicates" contract survives the change unaltered.

### 3.4 Constraint filtering

This library filters behaviors by marker-interface constraints (`where TRequest : ITransactionalRequest` on `SampleTransactionBehavior<,>`, and the same shape on the EF Core / NHibernate transaction behaviors). Under open-generic registration, Microsoft DI does that filtering by letting `MakeGenericType` throw `ArgumentException` and swallowing it when `throwOnConstraintViolation` is false.

> [!IMPORTANT]
> Under `IsDynamicCodeSupported=false` that filtering is **already broken**, independently of anything else. `VerifyOpenGenericAotCompatibility` runs *before* `MakeGenericType`, so a behavior whose constraint the request does not satisfy throws the value-type error instead of being silently skipped. The constrained-behavior path is not merely unsupported under AOT - it fails loudly for requests it was never meant to apply to.

Closed registration does not preserve the runtime filtering semantics; it **replaces** them with compile-time filtering, which is strictly better: the generator simply does not emit a registration for a pair whose constraints fail. Roslyn exposes exactly the needed API, and this repo's generator already has the machinery - `GenericConstraintChecker.Satisfies`, `GenericConstraintSnapshotBuilder`, and `OpenGenericHandlerResolver` in `src/Interlocutor.Generator/`. martinothamar/Mediator does the same thing in one line ([`PipelineBehaviorType.cs`](https://github.com/martinothamar/Mediator/blob/main/src/Mediator.SourceGenerator/Implementation/Analysis/PipelineBehaviorType.cs)):

```csharp
if (Symbol.SatisfiesConstraints([messageSymbol, responseSymbol], compilation))
{
    _messages.Add(message);
}
```

### 3.5 The second blocker nobody mentioned: the mediator's own wrapper construction

> [!CAUTION]
> **Under a genuine native binary the failure is earlier and different from the one reported.** DI is never reached. `Mediator.Send<TResponse>` fails first:
>
> ```
> System.NotSupportedException: 'Interlocutor.Abstractions.Queries.QueryHandlerWrapper`2[GetTodoByIdQuery, OperationOutcome`2[TodoItem, NotFound]]'
> is missing native code or metadata. …
>    at System.ActivatorImplementation.CreateInstance(Type, Boolean)
>    at Interlocutor.Abstractions.Mediator.Send[TResponse](IQuery`1, CancellationToken)
> ```
>
> *Empirically verified in this repo: `dotnet publish -c Release -r linux-x64` of the sample, then running the produced ELF binary.*

The cause is `src/Interlocutor.Abstractions/Mediator.cs:93-96` (and the identical shape at `:130-132` and `:164-166`):

```csharp
Activator.CreateInstance(
    typeof(QueryHandlerWrapper<,>).MakeGenericType(requestType, typeof(TResponse))
)!
```

ILCompiler's dataflow analysis *can* resolve a `MakeGenericType` whose arguments are all statically known - a probe confirmed `typeof(Wrapper<,>).MakeGenericType(typeof(A), typeof(T))` inside a generic method works natively when `T` is statically known at each call site. It cannot resolve this one, because `requestType` is `query.GetType()`: a runtime `Type` with no static identity.

Good news: **the generator already emits the replacement.** `src/Interlocutor.Generator/DispatchWrapperEmitter.cs` emits one closed static wrapper per handler whose own doc comment describes it as "the compile-time replacement for the runtime `Activator.CreateInstance(typeof(QueryHandlerWrapper<,>).MakeGenericType(…))` construction `Mediator.Send` performs today." It composes the onion using only compile-time-known closed types.

Bad news, twice over:

1. Those wrappers are wired only to the **runtime-typed** `Dispatch(object)` path via `IGeneratedRequestDispatcher`. The strongly-typed `sender.Send(new GetTodoByIdQuery(id), ct)` overload - the one the sample and every idiomatic caller uses - bypasses them and goes straight to the reflective path. Routing the typed overloads through the generated wrappers is a comparatively small change and would close this half.
2. The generated wrapper still resolves behaviors as `services.GetService(typeof(IEnumerable<IPipelineBehavior<Q,R>>))` (`DispatchWrapperEmitter.cs`, `EmitWrapper`). **That still hits the open-generic guard** unless the registrations are closed. So both halves are genuinely required; neither alone is sufficient.

---

## 4. How comparable libraries solve this

### martinothamar/Mediator - source-generated closed registrations

The README states the mechanism outright: "The source generator adds DI registrations manually as opposed to open generics registrations, to support NativeAOT. You can also manually add pipeline behaviors to the DI container if you are not doing AoT compilation." ([README §3.4](https://github.com/martinothamar/Mediator/blob/main/README.md))

The generator emits exactly one closed descriptor per (behavior × message) pair ([`PipelineBehaviorModel.cs:22-24`](https://github.com/martinothamar/Mediator/blob/f84bc5f115f283779373442c16c500ac365cdece/src/Mediator.SourceGenerator/Implementation/Models/PipelineBehaviorModel.cs#L22-L24)):

```csharp
var registration =
    $"services.Add(new {sd}(typeof({interfaceSymbol}<{requestType}, {responseType}>), typeof({concreteSymbol}{(type.Symbol.IsGenericType ? $"<{requestType}, {responseType}>" : "")}), {analyzer.ServiceLifetime}));";
```

Behavior-to-message matching is compile-time constraint evaluation (quoted in [§3.4](#34-constraint-filtering)).

> [!IMPORTANT]
> **The price Mediator pays is a design constraint this library has deliberately rejected.** Because the generator must know the behavior list at compile time, Mediator requires behaviors to be declared where a generator can read them - `MediatorOptions.PipelineBehaviors` as an assembly attribute or as inline `typeof(...)` expressions in `AddMediator`. Its own README carries a repeated note next to every runtime `services.AddSingleton(typeof(IPipelineBehavior<,>), …)` example: "NOTE: for NativeAOT, you should use the pipeline configuration on `MediatorOptions` instead." That is the direct antithesis of [ADR-0015](../adr/0015-pipeline-composition-behind-interlocutorpipelinebuilder.md), which puts pipeline composition behind a runtime fluent builder. [§6.2](#62-option-b-source-generated-closed-pipeline-composition) proposes a way to keep the builder anyway.

### JasperFx/wolverine - pre-generated code, opt-in Roslyn

Wolverine composes middleware by generating and compiling C# at runtime. Its docs state that "Pre-generated codegen is the prerequisite for publishing the app with Native AOT (`dotnet publish /p:PublishAot=true`)" and that removing Roslyn from production means running pre-built code in `TypeLoadMode.Static` ([Working with Code Generation](https://wolverinefx.net/guide/codegen)). In Wolverine 6 the Roslyn dependency moved out of core into an opt-in `WolverineFx.RuntimeCompilation` package, and the runtime-codegen surface is annotated `[RequiresDynamicCode]`/`[RequiresUnreferencedCode]`. Same conclusion as ours, reached from the opposite direction: **the composed chain must exist as compiled code before publish.**

### jbogard/MediatR - no AOT support, and no claim to it

`src/MediatR/MediatR.csproj` sets no `IsTrimmable`, `IsAotCompatible`, `EnableTrimAnalyzer`, or `EnableAotAnalyzer`, and the README makes no AOT or trim claim. The implementation is squarely reflection-based: `ServiceRegistrar.cs` registers `IPipelineBehavior<,>` as open generics and closes types with `MakeGenericType` in several places, and `Mediator.cs:59-60` uses the same `MakeGenericType` + `Activator.CreateInstance` wrapper pattern this library does. MediatR is not a counter-example - it is the incumbent ADR-0008 wants to differentiate from.

### FastEndpoints

`Src/Library/FastEndpoints.csproj` sets `<IsAotCompatible>true</IsAotCompatible>`. That is the analyzer-level guarantee described in [§5](#5-the-three-levels-are-not-the-same-guarantee) level (a), which is a weaker claim than "runs under `PublishAot`."

> [!NOTE]
> **TBD:** I did not trace FastEndpoints' pre/post-processor composition to determine whether it closes generics over value types at resolve time. Its response types are conventionally reference types, which would make the question moot for it - but that is inference, not verification.

---

## 5. Would a reference-type `OperationOutcome` fix it?

Mechanically, yes. Practically, it is not available.

### 5.1 The premise about reference-type sharing is correct

Reference-type instantiations share one canonical code body, which is why the docs describe only *struct* substitutions as needing per-instantiation pre-generation. The probes corroborate it: every reference-type case passed under native AOT, and `IsValueType` is the sole predicate the DI guard tests.

### 5.2 But a C# `union` is always a struct

> [!CAUTION]
> **There is no reference-type form of the C# `union` declaration in the preview this library depends on.** Verified empirically against SDK `11.0.100-preview.6.26359.118`:
>
> | Declaration | Result |
> | --- | --- |
> | `public readonly union U(A, B);` | compiles - `IsValueType=True`, base `System.ValueType`, sealed |
> | `public union U(A, B);` | compiles - `IsValueType=True`, base `System.ValueType`, sealed |
> | `public class union U(A, B);` | `error CS1514` / `CS1513` - not valid syntax |
> | `public union class U(A, B);` | `error CS1001` / `CS1514` - not valid syntax |
> | `public sealed union U(A, B);` | `error CS0106: The modifier 'sealed' is not valid for this item` |
>
> The `readonly` modifier is optional and changes nothing about the type's kind. `src/Interlocutor.CommonKernel/OperationOutcome.cs` declares every arity as `public readonly union OperationOutcome<...>(...) : IOperationOutcome`, which the compiler lowers to a sealed struct.

So option (C) is not "change `record struct` to `record class`." It is **abandon the `union` language feature and hand-write a closed class hierarchy** - which is precisely what [ADR-0001](../adr/0001-use-csharp-15-unions-for-result-types.md) chose unions over. The concrete losses:

- **Compiler-verified exhaustiveness.** A hand-written sealed hierarchy gets exhaustiveness only where the compiler can prove closure over an abstract base with `private protected` constructors; for the generic arities here (`OperationOutcome<TSuccess, TCase2, TCase3>` over open type parameters) that proof is not available, so switch expressions would need discard arms. This is the single load-bearing property of the whole result-type design per ADR-0001 and the README's "Result Types Are Building Blocks" section.
- **Implicit conversions from case types.** Reproducible by hand with `implicit operator`, but with the arity explosion the union syntax currently hides.
- **Struct equality and `default`.** Reference semantics change `==`, and `default` becomes `null` rather than the first case - a behavioural break for every consumer.
- **One allocation per dispatch**, per behavior hop, plus GC pressure - directly against the performance framing.
- **A breaking change to the library's most public type**, affecting every handler signature in every consumer codebase.

> [!NOTE]
> **TBD:** I could not find a csharplang proposal document covering the representation question. `proposals/standard-unions.md` is the only union file in [dotnet/csharplang `proposals/`](https://github.com/dotnet/csharplang/tree/main/proposals) and does not discuss struct-vs-class lowering. Whether a future `class union` form is planned is **unverified** - the table above establishes only what the pinned SDK accepts today. If a reference-type union form later ships, option (C) deserves re-examination on much better terms.

---

## 6. The three levels, and what each actually buys

### 6.1 The levels

**(a) Analyzer-clean library.** `<IsAotCompatible>true</IsAotCompatible>` turns on `IsTrimmable`, `EnableTrimAnalyzer`, `EnableSingleFileAnalyzer`, and `EnableAotAnalyzer` ([docs, AOT-compatibility analyzers](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/#aot-compatibility-analyzers)). This is a *compile-time promise about the library's own IL*. It says nothing about what happens at runtime, and it is trivially defeated by an inaccurate `UnconditionalSuppressMessage` - see [§8.2](#82-the-suppressions-currently-in-src-are-not-true).

**(b) Trimmed self-contained publish.** `PublishTrimmed=true` with the JIT still present. *Empirically verified in this repo*: the emitted runtimeconfig sets

```json
"Microsoft.Extensions.DependencyInjection.VerifyOpenGenericServiceTrimmability": true,
"System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported": true
```

Note the second line: **`PublishTrimmed` explicitly sets `IsDynamicCodeSupported` to `true`.** The AOT guard is therefore off, and every probe case - including open-generic Microsoft DI registration over `OperationOutcome<,>`, and Autofac `RegisterGeneric` over an instantiation present nowhere statically - passes. **Level (b) works today, on both containers, exactly as the premise claimed, and for exactly the reason claimed: the JIT can still synthesize value-type instantiations.**

**(c) Native AOT.** `PublishAot=true`, no JIT. Fails today at both points identified above.

### 6.2 What (c) buys over (b)

Measured on the sample in this dev container (*empirically verified*, time from process launch to Kestrel's "Application started" log line, two runs each):

| | Framework-dependent, JIT | Native AOT |
| --- | --- | --- |
| Startup | 462 ms / 437 ms | **58 ms / 58 ms** |
| Deployment | needs a .NET runtime installed | single 30 MiB self-contained ELF binary |

Roughly 7-8x faster cold start, no runtime install, and a hard guarantee of no runtime code generation - which matters for hardened, no-JIT, W^X-enforced container environments and for scale-to-zero serverless. Memory footprint was not measured.

Stopping at (b) costs the consumer that startup win and rules out no-JIT environments. It costs them nothing functionally: everything works.

---

## 7. CI story

This repo has no CI workflows today - only `.github/dependabot.yml`. Any recommendation here is greenfield.

### 7.1 The dev container can already do a real native publish

> [!IMPORTANT]
> **The premise that this container cannot native-compile is wrong.** `dotnet publish -c Release -r linux-x64` on the sample produced a working 30 MiB stripped ELF binary. The SDK probes for `clang`, does not find it, and falls back to `gcc`:
>
> ```
> command -v "clang"
> The command "command -v "clang"" exited with code 127.
> command -v "gcc"
> command -v "objcopy"
> LinkNative:
>   "gcc" ".../Interlocutor.Sample.WebApi.o" -o ".../Interlocutor.Sample.WebApi" … -fuse-ld=bfd … libz.a …
> ```
>
> *Empirically verified in this repo via `dotnet publish -v n`.* Everything the link needed - `gcc`, `objcopy`, `libz.a` - was already present. Every empirical native result in this document was produced this way. **All three levels can be gated locally and in CI without installing clang.** The .NET docs list `clang zlib1g-dev` as the Ubuntu prerequisite ([Prerequisites](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/#prerequisites)) and do not mention the gcc fallback; treat gcc as working-but-undocumented and pin clang in CI where you want a supported configuration.

### 7.2 A gate per level

| Level | Gate | Cost |
| --- | --- | --- |
| (a) analyzer-clean | `IsAotCompatible` on every `src/*` project + `dotnet build -warnaserror:IL2026,IL2069,IL3050,IL3002` | seconds, no toolchain |
| (a+) reference audit | add `<VerifyReferenceAotCompatibility>true</VerifyReferenceAotCompatibility>` - warns (IL3058) on any referenced assembly lacking `IsAotCompatible` metadata | seconds; noisy, so scope it to `src/` |
| (b) trimmed | `dotnet publish -p:PublishTrimmed=true -r linux-x64` on the sample **and run it against a smoke-test script** | ~1 min, no native toolchain |
| (c) native | `dotnet publish -r linux-x64` on the sample (with `PublishAot=true` in the csproj) **and run the produced binary** | ~2-4 min; needs gcc-or-clang + zlib + objcopy |

> [!WARNING]
> **A build-only gate is not a gate.** The single most important lesson from this investigation: the sample builds with `PublishAot=true` and zero analyzer warnings, and *every request throws at runtime*. `PublishAot` for analyzer purposes only catches nothing that matters here, because the failures are behind `UnconditionalSuppressMessage`. Whatever levels you gate, the gate must **execute** the published artifact and assert a successful dispatch.

> [!IMPORTANT]
> A full ILCompiler pass surfaces warnings the per-project build analyzer does not, because it sees the whole program. The sample's native publish reported, among others, `IL2069` on `InterlocutorRegistration`'s `ImplementationType` backing field and `IL3050` on `Mediator.ResolveSendMethod` - neither of which appears in `dotnet build`. **The claim that all library projects and the sample build with zero IL2xxx/IL3xxx warnings holds only for the per-project analyzer, not for ILC.** This is the third wrong premise.

### 7.3 Runner images

`ubuntu-24.04` GitHub-hosted runners ship Clang 16.0.6, 17.0.6, and 18.1.3 and gcc 13.2.0 preinstalled ([actions/runner-images `images/ubuntu/Ubuntu2404-Readme.md`](https://github.com/actions/runner-images/blob/main/images/ubuntu/Ubuntu2404-Readme.md)). No install step is needed for level (c) on GitHub-hosted Linux runners.

---

## 8. Corrections to the stated premises

### 8.1 Wrong premises

| Premise as stated | Reality |
| --- | --- |
| `VerifyOpenGenericServiceTrimmability` (or it and `IsDynamicCodeSupported` together) drive the throw | **Only `IsDynamicCodeSupported`.** `VerifyOpenGenericServiceTrimmability` is irrelevant to this guard, and `PublishTrimmed` actually sets `IsDynamicCodeSupported=true` ([§1.3](#13-which-feature-switch-drives-it), [§6.1](#61-the-levels)) |
| Everything builds with zero IL2xxx/IL3xxx warnings | True for the per-project analyzer; **false for ILCompiler**, which reports IL2069 and IL3050 in `src/` on a real native publish ([§7.2](#72-a-gate-per-level)) |
| The dev container has no clang, so a real native publish is out of reach | **A real native publish works**, with gcc as the linker driver ([§7.1](#71-the-dev-container-can-already-do-a-real-native-publish)) |

Also materially different from the framing given: under a **genuine** native binary the DI guard is never the first failure. `Mediator.Send`'s `Activator.CreateInstance`/`MakeGenericType` fails first ([§3.5](#35-the-second-blocker-nobody-mentioned-the-mediators-own-wrapper-construction)). Fixing only the DI registrations would move the error, not remove it.

### 8.2 The suppressions currently in `src/` are not true

Four `UnconditionalSuppressMessage` justifications assert something the type system contradicts:

- `src/Interlocutor.Abstractions/Mediator.cs:73-83`, `:111-118`, `:144-151`, `:545-553` - "QueryHandlerWrapper<,> is closed only over reference types (query and IOperationOutcome records/classes)"
- `src/Interlocutor.DependencyInjection.Autofac/ContainerBuilderExtensions.cs:128-134`, `:183-188` - "Every open generic Interlocutor registers … is closed only over reference types at resolution time"

`TResponse` is `OperationOutcome<...>`, which is a **struct** ([§5.2](#52-but-a-c-union-is-always-a-struct)). Universal reference-type generic sharing does not apply. These suppressions are what allow the analyzer-clean build to coexist with a runtime that fails on every dispatch. They should be corrected or removed regardless of which option below is chosen - they are actively misleading.

---

## 9. Options

### (A) Trim-compatible only; drop the Native AOT claim

**Evidence.** Level (b) works today, verified on both Microsoft DI and Autofac, for the documented reason ([§6.1](#61-the-levels)).

**Cost.** ADR-0008's Consequences must be amended: "the library becomes AOT- and trim-compatible" becomes trim-compatible, with AOT named as a non-goal or a future goal. The four false suppressions ([§8.2](#82-the-suppressions-currently-in-src-are-not-true)) must be replaced with honest `[RequiresDynamicCode]` annotations on `Mediator.Send` and the Autofac translator, so a consumer attempting `PublishAot` gets a build warning instead of a runtime crash. The sample's `PublishAot=true` should become `PublishTrimmed=true`. The differentiator against MediatR shrinks to "trim-clean and reflection-free discovery," which is still real but smaller.

**Buys.** Honesty, immediately, at near-zero engineering cost. Removes a claim the library cannot currently honour.

**Containers.** Microsoft DI and Autofac both verified working. Others presumptively fine at level (b), since the JIT does the work.

**Size.** Small. Documentation, a handful of attributes, one csproj property. Days, not weeks.

### (B) Source-generated closed pipeline composition

**Evidence.** Closed registrations pass under a genuine native binary on both Microsoft DI and Autofac ([§2](#2-other-containers-hit-the-same-wall---the-guard-is-not-the-cause), [§3.1](#31-microsoft-di-native-binary-closed-registration-works)); end-to-end in the real sample the DI guard disappears and the onion composes correctly ([§3.2](#32-end-to-end-in-the-sample-app)); the design is exactly what martinothamar/Mediator ships ([§4](#4-how-comparable-libraries-solve-this)); compile-time constraint filtering is strictly better than the runtime filtering it replaces, which is already broken under AOT anyway ([§3.4](#34-constraint-filtering)).

**What it requires.** Two changes, both needed:

1. **Closed behavior registrations.** The generator emits one closed `IPipelineBehavior<TRequest, TResponse>` descriptor per (behavior × request) pair whose constraints are satisfied, and the container translators stop emitting open-generic behavior descriptors.
2. **Typed dispatch through the generated wrappers.** Route `Mediator.Send<TResponse>(IQuery<TResponse>)` and its command siblings through the closed wrappers `DispatchWrapperEmitter` already produces, instead of `Activator.CreateInstance` + `MakeGenericType`.

**The design problem, and a way through it.** The generator cannot see the behavior list, because [ADR-0015](../adr/0015-pipeline-composition-behind-interlocutorpipelinebuilder.md) puts composition behind a *runtime* fluent builder. Mediator solved this by moving behavior declaration into generator-visible configuration - which would gut ADR-0015. The alternative that preserves it: have the generator **discover every `IPipelineBehavior<,>` implementation in the compilation and its references** (the same discovery it already does for handlers) and emit a closed descriptor for every constraint-satisfying (behavior × request) pair as a *catalog*. `InterlocutorPipelineBuilder` then keeps its full runtime API and simply selects and orders from that pre-generated catalog rather than emitting open generics. The fluent builder survives; the instantiations become statically visible. Behaviors invented at runtime (a `typeof(X<,>)` computed at startup, a plugin) become an explicitly unsupported-under-AOT path, annotated as such.

**Cost.** The largest option. The generator is ~3,300 lines today and already contains most of the needed parts - `GenericConstraintChecker`, `GenericConstraintSnapshotBuilder`, `OpenGenericHandlerResolver`, `RegistrationModuleEmitter`, `DispatchWrapperEmitter`. New work: behavior discovery, the (behavior × request) cross product with constraint filtering, catalog emission, translator changes in both containers, `Mediator` rewiring, and a genuinely-executing native CI gate. Realistically several weeks including tests. Generated-code volume grows as behaviors × requests, which the docs explicitly warn affects binary size.

**Also flags a real risk:** the catalog is combinatorial. A large application with a dozen behaviors and hundreds of requests emits thousands of descriptors. Mediator lives with this; it should be measured before committing.

**Buys.** The ADR-0008 claim becomes true. Genuine level-(c) support on Microsoft DI and Autofac, ~7-8x faster startup, no-JIT deployability, and the differentiator against MediatR that motivated the ADR in the first place.

**Containers.** Microsoft DI and Autofac verified. Any container that can accept a pre-closed registration inherits the fix.

### (C) Make `OperationOutcome` a reference type

**Evidence.** Reference-type sharing is real and would sidestep the problem entirely - but the C# `union` feature has no reference-type form, so this means abandoning unions for a hand-written hierarchy ([§5.2](#52-but-a-c-union-is-always-a-struct)).

**Cost.** Reverses ADR-0001. Loses compiler-verified exhaustiveness - the single property the whole result-type design exists for. Breaks `default`, `==`, and every consumer's handler signatures. Adds an allocation per dispatch per behavior hop.

**Buys.** A clean AOT story with no generator work.

**Recommendation: reject.** The cost lands on the library's defining feature to buy something option (B) buys without touching it. Worth revisiting only if a `class union` form ships in C#.

### (D) Hybrid - recommended

**Do (A) now, in full, as a correctness fix.** Correct the four false suppressions; annotate `Mediator.Send` and the Autofac translator with honest `[RequiresDynamicCode]`; amend ADR-0008's Consequences to claim trim-compatibility and name Native AOT as a tracked goal rather than a delivered one; switch the sample to `PublishTrimmed` with an **executing** smoke test; add the level-(a) and level-(b) CI gates from [§7.2](#72-a-gate-per-level). This is days of work and removes a claim the library currently cannot honour. Doing anything else first leaves a documented promise that crashes on every request.

**Then do (B) as a scoped follow-up**, sequenced so each step is independently verifiable:

1. Route typed `Send` overloads through the existing generated dispatch wrappers, removing the `Activator`/`MakeGenericType` path. Verify with an executing native gate that the `NotSupportedException` is gone.
2. Add behavior discovery + the constraint-filtered closed-registration catalog to the generator. Verify the DI guard is gone.
3. Measure generated-code volume on a realistically-sized application before committing to the cross-product shape.
4. Promote ADR-0008's AOT claim back to delivered, and add an executing level-(c) CI gate.

**Reject (C)** unless the C# union feature grows a reference-type form.

The reason for this sequencing rather than jumping to (B): the evidence for (B) is strong but the design question it raises - reconciling generator-visible behavior composition with ADR-0015's runtime builder - is genuinely unsettled, and (A) is a prerequisite for honesty regardless of whether (B) ever ships.

---

## 10. What remains unverified

> [!NOTE]
> Flagged deliberately, since a confidently-wrong conclusion here would be worse than an acknowledged gap.
>
> - **Lamar, DryIoc, and SimpleInjector were not tested.** [§2.2](#22-lamar-dryioc-and-simpleinjector) records only what their csproj files and READMEs say. Nothing in this document should be read as a per-container AOT verdict for them.
> - **FastEndpoints' middleware composition** was not traced ([§4](#4-how-comparable-libraries-solve-this)).
> - **No csharplang document was found** stating whether a reference-type union form is planned ([§5.2](#52-but-a-c-union-is-always-a-struct)). The table there reflects the pinned preview SDK's behaviour only.
> - **The combinatorial size of a (behavior × request) catalog was not measured** - it is an estimated risk for option (B), not a measured one.
> - **Memory footprint** of the native binary was not measured; only startup time and binary size were.
> - **The catalog design in option (B) is a proposal, not a validated design.** The empirical work proves closed registrations solve the runtime problem; it does not prove that this particular way of reconciling them with ADR-0015 is workable.
> - **All native results come from a gcc-linked binary**, which is not the documented configuration ([§7.1](#71-the-dev-container-can-already-do-a-real-native-publish)). A clang-linked build should behave identically, but that was not confirmed here.
