# Addendum B: Extracting a Reusable Library — Packaging, Naming, and Positioning

*Continuation of the PRD/ADR set. Nothing below revises a prior decision;
this addendum answers how the existing design should be split into
distributable packages if it's promoted from "application code" to a
shared library used across multiple services.*

## B.1 Should it be a single shared kernel?

**Partially.** One piece qualifies as a true shared kernel; the rest should
be satellite packages layered on top of it.

- **The kernel** is exactly the vocabulary that has zero dependency on any
  framework: `Result<T>`, `Result`, `Failure` and its case types
  (`ValidationFailed`, `RuleViolation`, `NotFound`, `Forbidden`,
  `Conflict`), `Success`, and `PagedResult<T>`. This is pure data — no EF
  Core, no ASP.NET, no FluentValidation, not even a dependency on the
  mediator abstractions. A console tool, a background worker, and a web API
  can all reference just this package.
- **Everything else should not be in the kernel**, because bundling it
  forces every consumer to take dependencies they may not want (a gRPC
  service has no use for `IActionResult` mapping; a batch job has no use
  for `ITransactionalRequest`/EF wiring).

This mirrors how mature ecosystems split (Serilog core + sinks,
`Microsoft.Extensions.Logging.Abstractions` + provider packages): one
dependency-free abstraction package, N adapter packages.

## B.2 Recommended package split

| Package | Contains | Depends on |
|---|---|---|
| **`Interlocutor`** | `Result<T>`, `Result`, `Failure`, `Success`, `ValidationFailed`, `RuleViolation`, `NotFound`, `Forbidden`, `Conflict`, `PagedResult<T>` | .NET BCL only |
| **`Interlocutor.Mediation`** | `IQuery<T>`, `ICommand<T>`, `ICommand`, `IPagedQuery<T>`, `IBulkCommand<TItem,TResponseItem>`, marker interfaces (`ITransactionalRequest`, `IExternalStoreRequest`, `IAuditableRequest`, `IRequiresPolicy`), `IPipelineBehavior<,>`, `IMediator`, the concrete `Mediator`, `ResourceCommandHandler<,,>`, and generic behaviors that need no *external* infrastructure (`LoggingBehavior`, `AuditingBehavior`, `AuthorizationBehavior`, `PagingValidationBehavior` — the last validates `IPagedQuery<T>.Page`/`PageSize` with plain bounds checks, no validation library needed) | `Interlocutor` |
| **`Interlocutor.Mediation.EntityFrameworkCore`** | `TransactionBehavior<TRequest,TResponse>` bound to `DbContext` | `Interlocutor.Mediation`, `Microsoft.EntityFrameworkCore` |
| **`Interlocutor.Mediation.FluentValidation`** | `ValidationBehavior<TRequest,TResponse>` bound to `IValidator<T>`, and `BulkItemValidationBehavior<TRequest,TItem,TResponseItem>` for `IBulkCommand<,>` (both need FluentValidation's `IValidator<T>`, so both live here rather than in the dependency-free mediation package) | `Interlocutor.Mediation`, `FluentValidation` |
| **`Interlocutor.AspNetCore`** | `ResultExtensions.ToActionResult(...)`, `ProblemDetails` mapping, minimal-API `TypedResults` mapping | `Interlocutor`, `Microsoft.AspNetCore.*` |

Application code (the `Widget`/`Crm` examples from earlier) is **not** part
of the library — it's the consumer, referencing whichever of the above
packages it needs.

## B.3 Naming: `Interlocutor`

**Proposed name: `Interlocutor`.**

Rationale:
- "Interlocutor" is the established legal/procedural term for *how a case
  was resolved* — dismissed, upheld, remanded — which maps precisely onto
  "how this request was resolved: succeeded, not found, forbidden,
  conflicted, failed validation." It reads naturally in code:
  `result.Interlocutor`, "the disposition of this command was `Forbidden`."
- It's unclaimed and unambiguous on NuGet, unlike heavily-used generic
  words (`Result`, `Outcome`) that collide with dozens of existing
  packages.
- It doesn't overload the word "Result," which C# already uses informally
  everywhere (`Task.Result`, LINQ `.Result()`), reducing naming collisions
  and confusing search results for consumers.

Namespace root: `Interlocutor`. Satellite packages append the integration
target: `Interlocutor.Mediation`, `Interlocutor.Mediation.EntityFrameworkCore`,
`Interlocutor.Mediation.FluentValidation`, `Interlocutor.AspNetCore`.

## B.4 What's defining about it — how it differs from existing packages

The space already has strong entries: `OneOf` (generic discriminated
unions), `FluentResults`/`Ardalis.Result`/`ErrorOr` (Result-pattern
libraries), and `MediatR` (mediator/pipeline library). None of them
occupies this design's specific position:

1. **Built on native C# unions, not a hand-rolled hierarchy or a generic
   `OneOfN<T0..Tn>`.** `OneOf` gives you exhaustiveness checking through a
   source generator and a positional `TN` type parameter list; this
   library uses the language's own `union` keyword, so case names are part
   of the type system natively, IDE tooling (find-usages, "add missing
   switch arms") works without a source generator, and there's no upper
   bound on case count imposed by a library's generated overloads.
2. **The Result vocabulary and the pipeline are co-designed, not
   independent.** `FluentResults`/`ErrorOr` give you a generic `Error`
   payload you populate however you like; `MediatR` has no opinion on
   result shape at all. Here, `ValidationBehavior` *is* the only producer
   of `ValidationFailed`, `AuthorizationBehavior`/`ResourceCommandHandler`
   *are* the only producers of `Forbidden` — the failure vocabulary isn't
   bolted on after the fact, it's the pipeline's own contract.
3. **`ValidationFailed` and `RuleViolation` are distinct, named cases.**
   Most Result libraries collapse "bad input" and "violated business rule"
   into one generic error type. Keeping them separate is what lets a single
   library-provided `ValidationBehavior` short-circuit before a
   transaction opens, while `RuleViolation` stays something only handler
   code can produce, after state has actually been examined.
4. **Resource-based authorization is a named, first-class pattern**
   (`ResourceCommandHandler<TCommand,TEntity,TResponse>`), not something
   every consuming team reinvents. No mainstream mediator library ships an
   opinion on "how do I authorize against an entity I haven't loaded yet."
5. **Opt-in via marker interfaces the request declares on itself**
   (`ITransactionalRequest`, `IExternalStoreRequest`, `IAuditableRequest`,
   `IRequiresPolicy`, `IPagedQuery<T>`, `IBulkCommand<,>`), never a central
   configuration list mapping request types to behaviors. This is a
   deliberate, consistent design rule across every satellite package, not
   just the core.
2. **A hard, enforced stance that exceptions are exceptional.** No behavior
   shipped in any package catches an arbitrary exception and converts it to
   a `Failure`. This is a constraint on the *library's own code*, not just
   documentation advice — consumers extending the pipeline inherit the same
   discipline by following the existing behaviors as examples.

## B.5 New interface-driven variants (detailed in code below)

Two additions make cross-cutting concerns reusable across *any* consuming
application without per-request boilerplate, which is the real test of
whether this is "a reusable library" versus "an application pattern
copy-pasted between repos":

- **`IPagedQuery<TItem> : IQuery<PagedResult<TItem>>`** exposes `Page`/
  `PageSize` as contract members, so the library can ship one generic
  `PagingValidationBehavior<TRequest,TItem>` that validates paging bounds
  for *every* paged query in *every* consuming application — no
  per-query validator required unless additional rules are needed.
- **`IBulkCommand<TItem,TResponseItem> : ICommand<IReadOnlyList<TResponseItem>>`**
  exposes `Items` as a contract member, so `Interlocutor.Mediation.FluentValidation`
  can ship one generic `BulkItemValidationBehavior<TRequest,TItem,TResponseItem>`
  that resolves `IValidator<TItem>` and validates every item automatically —
  no hand-written `RuleForEach` wrapper validator required unless the
  command needs cross-item rules (e.g. "no duplicate names within the
  batch"), in which case a normal `AbstractValidator<TRequest>` can still
  be registered alongside it.
