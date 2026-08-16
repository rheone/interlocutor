> **Amendment** - this ticket instructed citing ADR-0006 and ADR-0011 directly in the shipped `documentation/mediator-lifetimes-and-scope.md` prose. That instruction is superseded: `documentation/` files must state design rationale as fact and must never cite or link to `docs/adr/`, `docs/PRD.md`, or other `docs/` paths - see the house convention added in ticket 36. The shipped file has been corrected by ticket 36. No action needed here; historical context only.

## Labels

ready-for-agent

## What to build

A `documentation/mediator-lifetimes-and-scope.md` covering `IMediator`/`ISender`/`IPublisher` and how request scope works. Primary sources: ADR-0006 (interface segregation for mediator), ADR-0011 (scoped lifetime, non-configurable, root-scope guard), `docs/PRD.md` §10 (Mediator) and §14 (ambient context).

Cover:

- **`IMediator`, `ISender`, `IPublisher`**: what each interface segregates and why a consumer might depend on the narrower one.
- **Why the mediator is scoped, and deliberately non-configurable** - no escape hatch to make it singleton or transient - and what that buys (one DI scope, one dispatch, one consistent view of scoped dependencies like a `DbContext`/`ISession`).
- **The root-scope exception**: what triggers it and what it's protecting against.
- **Dispatching correctly from a background service** - since background services don't get a request-scoped `IServiceProvider` for free, show the pattern for creating a scope explicitly before dispatching.
- **Ambient context** (`IRequestContextAccessor`): `CorrelationId`, `UserId`, `TenantId`, `Items`, and that `RequestContext` is an immutable, swappable reference - how it flows through a dispatch and how a background service or queue consumer sets it up.

## Acceptance criteria

- [ ] `IMediator`/`ISender`/`IPublisher` segregation is explained with a concrete "why would I depend on the narrower interface" example
- [ ] Scoped lifetime rationale and the root-scope exception are explained
- [ ] A worked example shows correct mediator dispatch from a background service (explicit scope creation)
- [ ] `IRequestContextAccessor`/`RequestContext` is documented, including how a background service populates it
- [ ] Every code example compiles against the shipped API
- [ ] Follows house conventions from ticket 21

## Blocked by

21 - Establish documentation/ directory; retire CONTEXT.md
