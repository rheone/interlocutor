# 11 - Share GetManyAsync/GetPagedAsync between the NHibernate and EF Core repositories

**What to build:** `NHibernateRepository`'s and `EntityFrameworkRepository`'s `GetManyAsync` and `GetPagedAsync` methods are byte-for-byte identical - both operate only against `this.Query` plus `DomainRepositoryQueryableExtensions`, with nothing store-specific in either body. Move this shared implementation into `Interlocutor.Persistence.Abstractions` (e.g. as a shared base or extension method taking `IQueryable<TEntity>`), leaving only the genuinely store-specific members (`InsertAsync`, `UpdateAsync`, `DeleteAsync`) on each concrete repository type.

**Blocked by:** None - can start immediately

**Status:** done

- [x] `GetManyAsync` and `GetPagedAsync` have one shared implementation consumed by both `NHibernateRepository` and `EntityFrameworkRepository`
- [x] Neither store package gains a dependency on the other; both still depend only on `Persistence.Abstractions`
- [x] `Interlocutor.Persistence.NHibernate.Tests` and `Interlocutor.Persistence.EntityFrameworkCore.Tests` pass without change to their assertions

## Comments

**2026-09-04** - Verified on `2026-09-04-ResearchTicketNormalization` after merging `origin/2026-09-04-CodebaseAuditFollowUps` and `origin/ultrareview`: `DomainRepositoryBase` now owns `GetManyAsync`/`GetPagedAsync`; neither `NHibernateRepository` nor `EntityFrameworkRepository` redeclares them. Full solution `dotnet build` and `dotnet test` (1189/1189) pass post-merge.
