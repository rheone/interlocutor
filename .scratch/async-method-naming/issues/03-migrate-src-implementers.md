# 03 - Migrate src/ (non-Abstractions) implementers to the *Async dispatch members

**What to build:** Every handler, pipeline behavior, and mediator-adjacent implementation living outside `Interlocutor.Abstractions`/`Interlocutor.Persistence.Abstractions` switches over to implementing the new `*Async` members added in ticket 01, instead of the old ones.

**Blocked by:** 01 - Expand: add Async-suffixed dispatch members with forwarding defaults.

**Status:** done - implemented in `1e558c7`.

- [ ] Every implementation of the mediator dispatch interfaces in `Interlocutor.DependencyInjection.Microsoft`, `Interlocutor.DependencyInjection.Autofac`, `Interlocutor.FluentValidation`, `Interlocutor.Persistence.EntityFrameworkCore`, `Interlocutor.Persistence.NHibernate`, `Interlocutor.AspNetCore`, and any Generator-related runtime support overrides the new `*Async` member instead of the old one.
- [ ] Internal call sites within these packages call the `*Async` members rather than the old ones.
- [ ] Solution builds; all tests in the affected test projects pass with behavior unchanged.
