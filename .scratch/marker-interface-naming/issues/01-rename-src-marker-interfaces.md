# 01 - Rename the four marker interfaces in src/

**What to build:** Four marker interfaces in `Interlocutor.Abstractions` are renamed so all seven of the mediator's marker interfaces converge on one convention (adjective + `Request` suffix): `IRequiresPolicy` → `IRequiresPolicyRequest`, `ICancellable` → `ICancellableRequest`, `IHasTimeout` → `ITimeoutRequest`, `IRateLimited` → `IRateLimitedRequest`. `IAuditableRequest`, `ITransactionalRequest`, and `IExternalStoreRequest` already match the convention and are untouched. This is a straight rename, not an expand-contract - the project is unreleased, so there is no external-consumer compatibility surface to preserve.

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] The four interface declaration files under `src/Interlocutor.Abstractions/Markers/` are renamed (filename matches type name) and their type names updated: `IRequiresPolicy.cs` → `IRequiresPolicyRequest.cs`, `ICancellable.cs` → `ICancellableRequest.cs`, `IHasTimeout.cs` → `ITimeoutRequest.cs`, `IRateLimited.cs` → `IRateLimitedRequest.cs`. Each interface's XML doc summary/remarks are reworded only as needed to keep reading naturally under the new name (no content changes beyond the name).
- [ ] Every generic constraint and `is`/pattern-match check against the old names is updated: `AuthorizationBehavior<TRequest,_>`'s `where TRequest : IRequiresPolicy` constraint, `CancellationBehavior`'s constraint/checks, `TimeoutBehavior`'s constraint/checks (including `ITimeoutPolicy.ResolveBudget(IHasTimeout)`), `RateLimitedBehavior`'s constraint/checks.
- [ ] Every `<see cref>`/`<c>` doc reference to the old names is updated, including in `InterlocutorBuilder`, `InterlocutorPipelineBuilder`, `PipelineChainResolver`, `IPolicyEvaluator`, `IVoidCommandHandler`, and `OutcomeCaseRegistryExtensions`.
- [ ] `src/Interlocutor.Abstractions/PublicAPI.Unshipped.txt` reflects the new interface names (and the `PolicyName`/`LimitKey`/`Budget` member entries under them), with no leftover entries for the old names.
- [ ] `dotnet build` succeeds solution-wide with no new analyzer warnings, and no reference to the old four names remains anywhere under `src/` (`rg` for `IRequiresPolicy\b|ICancellable\b|IHasTimeout\b|IRateLimited\b` outside the new files returns nothing).
