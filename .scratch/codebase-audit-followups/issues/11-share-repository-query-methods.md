# 11 - Share GetManyAsync/GetPagedAsync between the NHibernate and EF Core repositories

**What to build:** `NHibernateRepository`'s and `EntityFrameworkRepository`'s `GetManyAsync` and `GetPagedAsync` methods are byte-for-byte identical - both operate only against `this.Query` plus `DomainRepositoryQueryableExtensions`, with nothing store-specific in either body. Move this shared implementation into `Interlocutor.Persistence.Abstractions` (e.g. as a shared base or extension method taking `IQueryable<TEntity>`), leaving only the genuinely store-specific members (`InsertAsync`, `UpdateAsync`, `DeleteAsync`) on each concrete repository type.

**Blocked by:** None - can start immediately

**Status:** ready-for-agent

- [ ] `GetManyAsync` and `GetPagedAsync` have one shared implementation consumed by both `NHibernateRepository` and `EntityFrameworkRepository`
- [ ] Neither store package gains a dependency on the other; both still depend only on `Persistence.Abstractions`
- [ ] `Interlocutor.Persistence.NHibernate.Tests` and `Interlocutor.Persistence.EntityFrameworkCore.Tests` pass without change to their assertions
