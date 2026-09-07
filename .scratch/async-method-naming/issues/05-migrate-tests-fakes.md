# 05 - Migrate tests/ fakes and doubles to the *Async dispatch members

**What to build:** Every test double, fake, or stub across the test suite that implements one of the mediator dispatch interfaces (or either internal dispatch-wrapper interface) switches to implementing the new `*Async` member.

**Blocked by:** 01 - Expand: add Async-suffixed dispatch members with forwarding defaults.

**Status:** ready-for-agent

- [ ] Every test double implementing `ICommandHandler<,>`, `IQueryHandler<,>`, `INotificationHandler<>`, `IPipelineBehavior<,>`, `ISender`, `IPublisher`, `IUnitOfWork`, or either dispatch-wrapper interface implements the `*Async` member instead of the old one.
- [ ] Actual `[Fact]`/`[Theory]` test method names are left untouched - renaming those is explicitly out of scope for this ticket.
- [ ] Full test suite passes.
