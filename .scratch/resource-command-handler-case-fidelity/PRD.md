# PRD: ResourceCommandHandler Case Fidelity

Status: open - issues 01-02 done (redesign adopted, recorded in [ADR-0027](../../maintainer_docs/adr/0027-resourcecommandhandler-eject-withcase.md)); issue 03 (doc/call-site sweep) not yet started.

## Problem

`ResourceCommandHandler<TCommand, TEntity, TSuccess>` fixes its response shape to `Outcome<TSuccess, NotFound, Forbidden>` - `TSuccess` names the one slot the type itself doesn't control, contrasted against two case types (`NotFound`, `Forbidden`) baked directly into the signature. That naming survived `.scratch/outcome-naming-and-arity-refactor/issue 08` deliberately, on the grounds that `TSuccess` here names a real, contractual payload for a fixed shape, not a positional template on an arbitrary union - unlike `Outcome<T1..TN>`'s own case-type parameters, which that issue renamed for exactly the opposite reason.

`AuthorizationBehavior<TRequest, TSuccess>` used to make the same trade for the same reason, and was carrying the same fixed-shape cost: pinning `Forbidden` into the signature broke Microsoft.Extensions.DependencyInjection's open-generic resolution (see ADR-0020), which is what actually forced the fix, not the naming. `pipeline-case-fidelity/07`'s `Eject.WithCase<TCase>` spike (committed in `fcd5654`, merged in `d4d22ce`) resolved it by going fully generic over `TResponse : IUnion` and deferring `Forbidden`'s construction to the dispatch machinery via a thrown, caught eject exception - `AuthorizationBehavior` no longer names or implies a privileged success/failure split at all.

`ResourceCommandHandler` is the one place in the shipped API that still fixes a response shape and names a slot after "success." Whether the same `Eject.WithCase` mechanism actually reaches it is an open question: `AuthorizationBehavior` is an `IPipelineBehavior<TRequest, TResponse>` inside the mediator's behavior chain, where the chain's own wrapper (`QueryHandlerWrapper`/`CommandHandlerWrapper`/generator-emitted equivalents) is the catch site that already knows `TResponse` concretely. `ResourceCommandHandler` is a template-method base class a concrete handler inherits from and explicitly re-exposes as `ICommandHandler<TCommand, TResponse>` (see `documentation/sample-app-walkthrough.md`'s note on why that explicit interface declaration is required) - not itself a pipeline participant, so it isn't proven that an exception it throws surfaces at the same catch site.

## Decision

Spike the mechanism against `ResourceCommandHandler` before committing to a shape (issue 01), record the outcome as an ADR either way rather than re-litigating it later (issue 02), then implement whichever shape was decided - either the full `Eject.WithCase` redesign or a fallback plain rename (`TSuccess` -> `TResponse`) that keeps the fixed shape - across every call site: the type itself, `PublicAPI.Unshipped.txt`, the doc pages that name it, the generator's `INTERLOC010` diagnostic remarks, and `TutorialProject`'s doc-comment mentions (issue 03).

`OutcomeMapperBase<TResult>`'s `Map<TFailure>` family (and `IOutcomeMapper<TResult>`/`ICustomFailureCaseRegistry<TResult>`/`CustomFailureCaseRegistryBuilder<TResult>`) was considered in the same design conversation and explicitly kept as-is: "failure case" is established glossary vocabulary for that family (see `documentation/glossary.md`'s "Failure Case Types" section), and every one of its uses genuinely is a failure case - the family's whole job is mapping the non-success side of an outcome. No ticket tracks that decision because no code changes.

## Verification

- `dotnet build` and `dotnet test` pass for the whole solution; `dotnet csharpier .` produces no diff.
- `TutorialProject`'s `ResourceCommandHandler` consumers (`CompleteTodoTaskHandler`-adjacent commands that do use the base class, per `documentation/sample-app-walkthrough.md`) still compile and their tests pass unchanged in observed behavior.
- Whichever shape is chosen, every doc page and doc comment naming `ResourceCommandHandler<TCommand, TEntity, TSuccess>` is updated to match the shipped signature - no stale type-parameter name survives outside deliberately historical/contrastive prose (the precedent being `AuthorizationBehavior.cs`'s own remarks, which keep narrating its retired `TSuccess` shape on purpose).
