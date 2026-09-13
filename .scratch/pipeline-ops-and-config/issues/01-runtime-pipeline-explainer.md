# 01 - Runtime pipeline explainer

**Status:** ready-for-agent

**What to build:** A diagnostic service that answers, against a live, already-built `IServiceProvider`, which pipeline behaviors resolve for a given request/response type pair - the runtime counterpart to `InterlocutorBuilder.DescribeRegistrations()`, which only reports the builder's own pre-container-build configuration.

`PipelineChainResolver.ResolveChain<TRequest,TResponse>(IServiceProvider)` (`src/Interlocutor.Abstractions/Pipeline/PipelineChainResolver.cs`) already computes the real answer - it's public, and both dispatch paths (the ordinary typed API and the generator-emitted runtime-typed wrappers) already call it. Do not duplicate its logic. What's missing is a friendlier, documented entry point for a human (a support engineer, an admin endpoint, a test) who has a `Type` in hand - typically from a bug report or a config file, not a compile-time generic argument - and wants a formatted answer without writing a throwaway console app that calls the generic method via reflection themselves.

Add, in `Interlocutor.Abstractions.Pipeline.Diagnostics` (new namespace):

```csharp
public interface IPipelineExplainer
{
    PipelineExplanation Explain(Type requestType, Type responseType, IServiceProvider services);
}

public sealed record PipelineExplanation(
    Type RequestType,
    Type ResponseType,
    IReadOnlyList<Type> ResolvedBehaviorTypes,
    string? MatchedOverrideRuleName);
```

The default implementation resolves `requestType`/`responseType` into `PipelineChainResolver.ResolveChain<,>` via `MakeGenericMethod` (reflection is acceptable here - this is an explicitly opt-in diagnostic path, not the hot dispatch path, and should be documented as such alongside the existing trim/AOT caveats other reflective builder methods already carry) and projects each resolved behavior instance to `.GetType()`.

`MatchedOverrideRuleName` needs `IPipelineOverrideRegistry.TryGetOverride` (`src/Interlocutor.DependencyInjection.Abstractions/PipelineOverrideRegistry.cs`) to also surface which rule's `Name` matched, not just its `BehaviorTypes` - `PipelineOverrideRule.Name` already exists for exactly this kind of identification (today only consumed by `ConflictingPipelineOverride` messages). **Decided:** change the method's return shape to a small record instead of the current `bool` + `out IReadOnlyList<Type>` pair - one source of truth, not a second parallel lookup:

```csharp
public sealed record PipelineOverrideMatch(IReadOnlyList<Type> BehaviorTypes, string RuleName);

public interface IPipelineOverrideRegistry
{
    PipelineOverrideMatch? TryGetOverride(Type requestType); // null = no match (was: bool + out)
}
```

This is a breaking signature change to a public interface - update the sole existing implementation (`PipelineOverrideRegistry`) and the sole existing consumer (`PipelineChainResolver.ResolveChain`, `src/Interlocutor.Abstractions/Pipeline/PipelineChainResolver.cs`) in the same change, plus every test exercising either.

**Enablement is opt-in and off by default.** This resolves live instances from the container and reveals internal wiring - not something to run unexamined against a production container. Gate it behind a settings object:

```csharp
public sealed class PipelineExplainerOptions
{
    public bool Enabled { get; set; } // default false
}
```

Registration surface: `builder.Pipeline.AddPipelineExplainer(PipelineExplainerOptions? options = null)` on the container-neutral core, registering `IPipelineExplainer` (a no-op/throwing implementation when `Enabled` is false - decide which behavior is more useful and document it). Under `Interlocutor.DependencyInjection.Microsoft`, if an `IOptions<PipelineExplainerOptions>` or `IOptionsMonitor<PipelineExplainerOptions>` is already registered (e.g. via `services.Configure<PipelineExplainerOptions>(configuration.GetSection("Interlocutor:PipelineExplainer"))`), prefer it over the builder-supplied value - consistent with how `Defaults` already lets a consumer's own registration win.

**Acceptance criteria:**
- [ ] `IPipelineExplainer.Explain` returns the correct ordered behavior-type list for a request type resolving through the Tier-2 general-purpose baseline
- [ ] `IPipelineExplainer.Explain` returns the correct ordered behavior-type list and the matching rule's `Name` for a request type matched by a Tier-1 `OverridePipelineFor` rule
- [ ] `IPipelineOverrideRegistry.TryGetOverride` (or a documented sibling) exposes the matched rule's name without breaking existing callers
- [ ] Disabled by default (`PipelineExplainerOptions.Enabled == false`); calling `Explain` while disabled throws `InvalidOperationException` with an actionable message (e.g., naming the option to set to enable it) - matching this codebase's existing "throw with an actionable message" precedent (see `ResolveServiceType`'s exception)
- [ ] `IPipelineOverrideRegistry.TryGetOverride` returns `PipelineOverrideMatch?` (breaking signature change), with `PipelineOverrideRegistry` and `PipelineChainResolver.ResolveChain` both updated to match
- [ ] Under Microsoft DI, an already-registered `IOptions<PipelineExplainerOptions>`/`IOptionsMonitor<PipelineExplainerOptions>` wins over the builder-supplied value
- [ ] Autofac gets the identical POCO-configured capability with no `IOptions` dependency
- [ ] `documentation/pipeline-and-behaviors.md` documents the explainer, its opt-in-and-disabled-by-default posture, and the reflection/trim caveat
- [ ] New unit tests cover: Tier-2 resolution, Tier-1 override resolution (with rule name via `PipelineOverrideMatch`), disabled-by-default throw behavior, and the Microsoft-DI options-override-wins precedence

**Blocked by:** None - can start immediately.
