# 09 - Close `DependencyTests.cs` layering gaps

**What to build:** `DependencyTests.cs` actually enforces the layering rules CLAUDE.md and the previously-closed `.scratch/audit-followups/issues/06-...` ticket claim are covered: neither store-specific persistence package may depend on `Interlocutor.FluentValidation`, and `Interlocutor.CommonKernel` is checked against every current sibling package, not just the original five.

Full technical detail: [findings/08-dependencytests-layering-gaps.md](../findings/08-dependencytests-layering-gaps.md).

**Blocked by:** None - can start immediately.

**Status:** done

- [x] A test asserts `Interlocutor.Persistence.EntityFrameworkCore` does not depend on `Interlocutor.FluentValidation`
- [x] A test asserts `Interlocutor.Persistence.NHibernate` does not depend on `Interlocutor.FluentValidation`
- [x] `CommonKernel`'s negative-dependency test includes `Interlocutor.DependencyInjection.Abstractions`, `.Microsoft`, `.Autofac`, and `Interlocutor.Persistence.Abstractions`
- [x] `.scratch/audit-followups/issues/06-complete-dependencytests-layering-assertions.md` is reopened or annotated to reflect that its criteria weren't durable across later package additions

## Comments

**2026-09-04** - Re-triaged while working the frontier: already implemented in commit `6a3f9ce`. DependencyTests.cs now asserts both Interlocutor.Persistence.EntityFrameworkCore and .NHibernate don't depend on Interlocutor.FluentValidation, and CommonKernel's negative-dependency test covers the DI packages and Persistence.Abstractions. No source changes were needed this session.
