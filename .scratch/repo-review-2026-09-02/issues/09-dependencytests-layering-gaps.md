# 09 - Close `DependencyTests.cs` layering gaps

**What to build:** `DependencyTests.cs` actually enforces the layering rules CLAUDE.md and the previously-closed `.scratch/audit-followups/issues/06-...` ticket claim are covered: neither store-specific persistence package may depend on `Interlocutor.FluentValidation`, and `Interlocutor.CommonKernel` is checked against every current sibling package, not just the original five.

Full technical detail: [findings/08-dependencytests-layering-gaps.md](../findings/08-dependencytests-layering-gaps.md).

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] A test asserts `Interlocutor.Persistence.EntityFrameworkCore` does not depend on `Interlocutor.FluentValidation`
- [ ] A test asserts `Interlocutor.Persistence.NHibernate` does not depend on `Interlocutor.FluentValidation`
- [ ] `CommonKernel`'s negative-dependency test includes `Interlocutor.DependencyInjection.Abstractions`, `.Microsoft`, `.Autofac`, and `Interlocutor.Persistence.Abstractions`
- [ ] `.scratch/audit-followups/issues/06-complete-dependencytests-layering-assertions.md` is reopened or annotated to reflect that its criteria weren't durable across later package additions
