# 01 - Expand: add Async-suffixed dispatch members with forwarding defaults

**What to build:** Every mediator dispatch interface gains a new, Async-suffixed member alongside its existing one, so callers can start using the new name immediately while every existing implementer keeps compiling and behaving exactly as before. This is the "expand" half of a wide interface-member rename (`Handle`/`Send`/`Publish`/`Cancel` and the two internal dispatch-wrapper interfaces' `Handle`) — nothing is removed yet, nothing breaks.

**Blocked by:** None - can start immediately.

**Status:** done - implemented in `bf00353`.

- [ ] `ICommandHandler<,>`, `IQueryHandler<,>`, `INotificationHandler<>`, and `IPipelineBehavior<,>` each gain a `HandleAsync` member; `ISender` gains `SendAsync` (all existing overloads); `IPublisher` gains `PublishAsync` (all existing overloads); `IUnitOfWork` gains `CancelAsync`; the two internal dispatch-wrapper interfaces gain `HandleAsync`.
- [ ] Each new member is a default interface implementation whose body forwards to the corresponding existing (unchanged) member - no implementer anywhere in the solution needs to change, and the solution builds and all existing tests pass exactly as before this ticket.
- [ ] Calling a new `*Async` member on any existing handler/mediator/unit-of-work instance produces the identical outcome to calling the old member it forwards to.
- [ ] The new members carry rich XML doc comments (per this repo's documentation convention) describing them as the preferred name going forward; the old members' doc comments note they are being phased out in favor of the `*Async` form (no `[Obsolete]` attribute yet - that would warn every caller mid-migration, which is premature here).
