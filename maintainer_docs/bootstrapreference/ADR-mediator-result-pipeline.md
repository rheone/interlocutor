# Architecture Decision Records: Mediator Result Pipeline

Each record follows: Context → Decision → Consequences → Alternatives
considered. Status is `Accepted` unless noted.

---

## ADR-0001: Adopt C# 15 union types for Result modeling

**Context**
Handlers need to communicate a closed set of expected outcomes (success
value, or one of several named failures) in a way the compiler can check
exhaustively. Prior options - `object` returns, boolean flags with out
parameters, third-party libraries (e.g. `OneOf`), or throwing exceptions
for expected outcomes - each fail at least one of: compile-time
exhaustiveness, discoverability from the signature, or freedom from an
external dependency.

**Decision**
Use the C# 15 `union` keyword (available from .NET 11 Preview 2/3) to
declare `Result<T>`, `Result`, and `Failure` as closed unions of record
case types.

**Consequences**
- Pro: compiler-enforced exhaustive `switch` at every consumption site;
  adding a new failure case surfaces every place that needs updating.
- Pro: no external dependency (replaces what `OneOf` would have provided).
- Con: language surface is preview-only; syntax may shift before GA
  (~November 2026). Mitigated by isolating all `union` declarations to a
  single type-catalog file.
- Con: compiler-generated unions are structs that box their `Value` -
  acceptable overhead for a mediator layer, flagged as a future
  optimization point if needed (non-boxing custom union pattern is
  available).

**Alternatives considered**
- `OneOf<T0,...>` (NuGet): runtime-checked, not compiler-exhaustive;
  dropped once native unions became available in preview.
- Custom closed hierarchy via sealed abstract record + pattern matching:
  works today without preview features, but requires shared base type and
  loses the "any type can be a case" flexibility. Considered a fallback if
  union syntax churns too much before GA.

---

## ADR-0002: Separate shape validation (`ValidationFailed`) from business rule violations (`RuleViolation`)

**Context**
"Invalid input" was originally a single bucket. In practice, structural
input problems (missing field, malformed value) and domain-logic problems
(insufficient balance, invalid state transition) have different causes,
different pipeline stages (pre-lookup vs post-lookup), and different
callers who care about them (client-side form validation vs domain
logging/alerting).

**Decision**
Model them as two distinct `Failure` cases: `ValidationFailed` (a map of
field → list of `(Code, Message)`) produced by shape validation before any
entity lookup, and `RuleViolation` (`Code`, `Message`) produced by handler
business logic after lookup.

**Consequences**
- Pro: `ValidationBehavior` can short-circuit cheaply before opening a
  transaction or touching the database.
- Pro: HTTP mapping differs meaningfully (400 Bad Request vs 422
  Unprocessable Entity), which wouldn't be possible with one merged case.
- Con: slightly more types to maintain; judged worth it for the clarity.

**Alternatives considered**
- Single `ValidationFailed` case covering both: rejected, conflates two
  different pipeline stages and HTTP semantics.

---

## ADR-0003: Resource-based authorization via template method, not a pipeline behavior

**Context**
Authorization splits into two kinds: policy/role checks that need no
loaded data (non-resource), and ownership/claims checks that need the
*fetched entity* (resource-based). Generic pipeline behaviors operate
before the handler runs and have no natural place to receive data the
handler's lookup step produces, short of introducing a shared mutable
context object - which conflicts with the goal of keeping all result types
and pipeline plumbing immutable/stateless.

**Decision**
Non-resource authorization is a generic `IPipelineBehavior`, no-op unless
the request implements `IRequiresPolicyRequest`. Resource-based authorization is
implemented as a step inside `ResourceCommandHandler<TCommand,TEntity,
TResponse>` (template method): `LoadAsync` → `Authorize` → `HandleAsync`,
all sequential, all local to one method call, no shared context.

**Consequences**
- Pro: no mutable state threaded through the pipeline.
- Pro: authorization logic lives next to the lookup it depends on, easy to
  reason about and test as one unit.
- Con: resource-based authorization isn't a swappable pipeline layer - it's
  fixed to the template method's shape. Acceptable since resource
  authorization is inherently handler-specific, not a generic cross-cutting
  concern.

**Alternatives considered**
- Shared per-request mutable context (`RequestContext` bag) populated by a
  `LookupBehavior` and read by an `AuthorizationBehavior`: rejected -
  reintroduces mutable shared state and an implicit ordering dependency
  between behaviors that the compiler can't verify.
- Re-fetching the entity inside a generic authorization behavior: rejected
  - duplicate query per request, and still needs the entity type as a
  generic parameter, cutting against genericity.

---

## ADR-0004: Onion-model pipeline behaviors for cross-cutting concerns

**Context**
Logging, auditing, shape validation, non-resource authorization, and
transactions all need to run "around" every handler, some only doing
pre-work, some only post-work, some both.

**Decision**
Use a single `IPipelineBehavior<TRequest,TResponse>` abstraction per
concern, each wrapping a `next()` delegate (Russian-doll / onion model, a
specialization of Chain of Responsibility). Order: tracing → logging →
auditing → shape validation → non-resource authorization → transaction →
handler.

**Consequences**
- Pro: one behavior class expresses both "on the way in" and "on the way
  out" logic around a single `next()` call - no separate pre/post
  registries to keep in sync.
- Pro: behaviors are generic over `<TRequest,TResponse>` and never need to
  know `T` - `Result<T>` flows through untouched.
- Con: execution order depends on DI registration order, which isn't
  guaranteed identical across containers - mitigated by a pinned
  integration test (ADR-0008 also addresses opt-in correctness).

**Alternatives considered**
- Separate `IPreProcessor`/`IPostProcessor` interfaces (as MediatR
  historically offered): rejected as an added abstraction with no benefit
  over a single wrapping delegate, and harder to guarantee pre/post run in
  matching pairs.

---

## ADR-0005: Exceptions reserved for exceptional conditions only

**Context**
It's tempting to add a pipeline behavior that catches all exceptions and
folds them into a `Failure` case, for a "uniform" result contract. This
would blur the line between bugs/infrastructure faults and legitimate
domain outcomes - the exact distinction this design exists to preserve.

**Decision**
No behavior or handler catches and converts arbitrary exceptions into
`Result`/`Failure`. Unhandled exceptions propagate out of the mediator
pipeline entirely and are handled once, centrally, by ASP.NET's
exception-handling middleware (`IExceptionHandler`), which maps them to a
generic 500 `ProblemDetails` response.

**Consequences**
- Pro: `Failure` stays a trustworthy, closed set of *expected* outcomes;
  seeing a `Failure` case in logs/telemetry never means "something broke."
- Pro: bugs surface as exceptions/alerts, not silently as a domain outcome
  a caller might handle as normal.
- Con: none identified; this is the behavior the original design goal
  ("exceptions should only be thrown from exceptional events") requires.

**Alternatives considered**
- Global "exception → Failure" behavior: rejected per above.

---

## ADR-0006: Single transactional scope (Unit of Work) per mediator request

**Context**
A `ResourceCommandHandler`'s lookup and its subsequent write must observe
a consistent view of data and commit atomically - the session used to load
the entity must be the same session the write is applied through and
committed on.

**Decision**
The session/`DbContext` is registered with **Scoped** DI lifetime, matching
one mediator dispatch (one HTTP request). `TransactionBehavior` begins a
transaction on that scoped session (only for requests implementing
`ITransactionalRequest`), calls `next()`, and commits (calling
`SaveChanges` once) only if the resulting `Result`/`Result<T>` is not a
`Failure`; otherwise it rolls back. `ResourceCommandHandler` receives the
*same* scoped session via constructor injection for both `LoadAsync` and
any writes performed in `HandleAsync`.

**Consequences**
- Pro: atomicity guaranteed by DI scope lifetime, not by manual
  coordination - no risk of lookup and write using different connections.
- Pro: commit/rollback decision is centralized and driven directly by the
  `Result` type - a `Conflict` or `RuleViolation` rolls back exactly like a
  thrown exception would, with no special-casing.
- Con: requires discipline that all repositories/read stores used inside a
  transactional request resolve the same scoped session - standard ASP.NET
  DI behavior within a request, but worth an explicit test for
  background/queue-triggered dispatches that build scopes manually.

**Alternatives considered**
- Handler-managed transactions (each handler opens/commits its own): rejected
  - duplicates transaction logic per handler and makes "did we roll back on
  every failure path" a per-handler audit instead of a guarantee.

---

## ADR-0007: `CancellationToken` defaulted only at the outer boundary

**Context**
C# resolves default parameter values at the compile-time/static type of
the call site, not virtually. If `CancellationToken ct = default` appears
on an interface method, calling through the interface reference and
calling through a concrete class reference can silently use different
defaults, which is a common source of subtle bugs in handler pipelines.

**Decision**
`CancellationToken` is a required parameter on every handler and behavior
method (`IQueryHandler`, `ICommandHandler`, `IPipelineBehavior`,
`ResourceCommandHandler`). The only place a default value is offered is
`IMediator.Send(..., CancellationToken ct = default)`, the single public
entry point.

**Consequences**
- Pro: eliminates a whole class of "which default fired" bugs.
- Pro: forces every internal layer to explicitly propagate the token,
  making cancellation-awareness auditable by reading signatures.
- Con: mildly more verbose internal signatures; judged worth the safety.

**Alternatives considered**
- Default on every layer: rejected per the dispatch-ambiguity risk above.

---

## ADR-0008: Marker interfaces for opt-in cross-cutting behaviors

**Context**
Not every request needs every cross-cutting concern - queries don't need
auditing or transactions; many commands don't need a named authorization
policy check. Behaviors need a way to know whether they apply without a
central configuration list that's easy to forget to update.

**Decision**
Use marker/capability interfaces declared on the request type itself:
`ITransactionalRequest`, `IAuditableRequest` (carries `ActionName`),
`IRequiresPolicyRequest` (carries `PolicyName`). Each corresponding behavior
checks `request is TMarker` and no-ops (calls `next()` directly) when
absent.

**Consequences**
- Pro: opt-in is visible at the request type's declaration - reading the
  `record` or `class` for a command tells you which concerns apply, no need
  to check pipeline registration.
- Pro: no reflection-based convention or naming-pattern guessing.
- Con: nothing compiler-enforced ensures the *correct* commands implement
  the *correct* markers (e.g. a new write command forgetting
  `ITransactionalRequest`). Recommend an architecture test or Roslyn
  analyzer asserting all `ICommand` implementers also implement
  `ITransactionalRequest` unless explicitly exempted via a documented
  opt-out marker (e.g. `INonTransactional`).

**Alternatives considered**
- Central per-request-type configuration table mapping request → behaviors:
  rejected - moves the opt-in decision away from the type definition,
  easy to drift out of sync.

---

## Addendum: ADR-0009 through ADR-0011

*These extend the record set above and do not revise or supersede any
prior ADR. All prior decisions (union-based `Result`, template-method
resource authorization, onion pipeline, exceptions-only-for-bugs, single
transactional scope, required `CancellationToken`, marker-interface opt-in)
remain in force.*

---

## ADR-0009: Lazy, paired-marker session resolution for non-database-backed requests

**Context**
ADR-0006/ADR-0008 established `ITransactionalRequest` as the opt-in for the
SQL unit-of-work. In practice some commands and queries are backed by an
external API or a non-relational store and must **never** cause a SQL
session to be constructed - not just skip the transaction, since some
drivers/stores open connections eagerly at construction, and even where
they don't, resolving an unused `AppDbContext` obscures intent and can
mask accidental SQL usage in code review.

**Decision**
`TransactionBehavior` depends on `Lazy<AppDbContext>` rather than
`AppDbContext` directly, so the session is constructed only if
`.Value` is actually evaluated - which happens only inside the
`ITransactionalRequest` branch. A second marker, `IExternalStoreRequest`,
is added purely to make the "deliberately no SQL" choice an explicit,
reviewable declaration rather than an inferred absence. An architecture
test requires every concrete `ICommand`/`ICommand<T>` to implement exactly
one of the two markers.

**Consequences**
- Pro: SQL session is provably never constructed for external-store
  requests - verifiable by inspecting `TransactionBehavior`'s single
  branch, not by auditing every handler.
- Pro: the choice is visible at the command's type declaration, consistent
  with the marker-interface philosophy of ADR-0008.
- Con: adds one more required interface per command type; judged worth it
  given the alternative (silent inference from absence) is exactly what
  ADR-0008 already rejected for other concerns.

**Alternatives considered**
- Inferring "no DB" purely from the absence of `ITransactionalRequest`:
  rejected - indistinguishable from "forgot to add the marker," and
  provides no positive signal for review or tooling.
- A single `IStoreBackedRequest<TStore>` generic marker naming the store
  type: rejected as unnecessary complexity - the pipeline only needs to
  know "SQL session: yes or no," not which specific external store is in
  play; that detail belongs to the handler's own constructor-injected
  client.

---

## ADR-0010: Materialized collection results via a dedicated `PagedResult<T>` type

**Context**
`Result<T>` (ADR-0001) was defined generically enough to already
accommodate collection payloads, but two risks needed an explicit decision:
deferred-execution payloads (`IEnumerable<T>`/`IQueryable<T>`) can outlive
the session that would materialize them once handed back up through the
pipeline, and paging metadata (page, size, total count) has nowhere to
live in a bare `IReadOnlyList<T>`.

**Decision**
Success payloads are always materialized before being returned from a
handler. A new record, `PagedResult<T>(IReadOnlyList<T> Items, int Page,
int PageSize, long TotalCount)`, is added to the type catalog and used as
the `T` in `Result<PagedResult<T>>` for paged queries. It requires no
change to `Failure`, `Result<T>`'s definition, or any existing pipeline
behavior, since every behavior is already generic over the opaque
`TResponse` (a direct consequence of ADR-0004's design).

**Consequences**
- Pro: no risk of a disposed-session exception surfacing after the
  pipeline believes the request succeeded.
- Pro: zero pipeline changes required - confirms the original genericity
  goal from ADR-0004 holds under a new payload shape.
- Pro: empty results stay unambiguously "success with zero items," keeping
  `NotFound` reserved for its original meaning (a specific resource-by-id
  missing), not overloaded to mean "empty result set."
- Con: none identified - purely additive.

**Alternatives considered**
- Returning `IAsyncEnumerable<T>` for streaming: rejected as the default -
  streaming and the "commit/rollback as one unit" transactional model
  (ADR-0006) don't compose cleanly; a genuinely necessary streaming export
  should be a distinct, separately-designed endpoint contract, not routed
  through `Result<T>`.
- Encoding paging metadata as HTTP headers only, with `Result<T>` staying
  `IReadOnlyList<T>`: rejected - keeps paging state out of the type the
  compiler checks, and duplicates total-count logic at the API layer
  instead of the query handler where it's naturally computed.

---

## ADR-0011: Validator composition via open-generic pipeline behavior, unchanged for collection payloads

**Context**
`ValidationBehavior<TRequest,TResponse>` (introduced under ADR-0004's
pipeline model) needed to be confirmed as sufficient for both scalar
commands/queries and collection-shaped ones (bulk commands, paged
queries), without introducing a second validation mechanism for the
collection case.

**Decision**
No new behavior or interface. `ValidationBehavior` continues to depend
only on `IEnumerable<IValidator<TRequest>>`, resolved by FluentValidation's
own DI registration (`AddValidatorsFromAssemblyContaining`). Collection
payloads inside a request (e.g. `BulkCreateWidgetsCommand.Items`) are
validated using FluentValidation's `RuleForEach(...).SetValidator(...)`,
with the item validator itself resolved from DI (constructor-injected into
the parent validator) so it remains reusable if the item type is ever
validated on its own.

**Consequences**
- Pro: confirms `TResponse`-genericity (ADR-0004) extends to validation as
  well as to the rest of the pipeline - one mechanism for scalar, bulk,
  and paged requests alike.
- Pro: item-level validators stay independently testable and reusable.
- Con: none identified - this ADR is primarily a confirmation that the
  existing design already covers the case, recorded so the question isn't
  re-litigated later.

**Alternatives considered**
- A dedicated `ICollectionValidationBehavior<TItem>` for bulk requests:
  rejected as unnecessary - FluentValidation's `RuleForEach` already
  expresses this within the existing `IValidator<TRequest>` contract with
  no pipeline changes.
