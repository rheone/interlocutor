# 07 - Move ADR-0018 citations out of shipped public XML docs

**What to build:** `IUnitOfWork.cs`, both `UnitOfWorkBehavior.cs` files (NHibernate and EF Core), and `UnitOfWorkSaveChangesInterceptor.cs` embed `<see href="../../maintainer_docs/adr/0018-...">ADR-0018</see>` links directly in their public XML doc comments. These ship to real consumers via IntelliSense/NuGet and are dead links outside the repo. Replace each citation with the rationale stated inline as fact, the way `Page<T>`'s own remarks already do correctly - do not link to `maintainer_docs/` from any shipped public doc comment.

**Blocked by:** None - can start immediately

**Status:** done

- [x] No public XML doc comment in `src/` links to `maintainer_docs/adr/` or any other `maintainer_docs/` path
- [x] Each affected doc comment states the underlying rationale inline instead
- [x] `dotnet build` succeeds with no new doc warnings

## Comments

**2026-09-04** - Verified on `2026-09-04-ResearchTicketNormalization` after merging `origin/2026-09-04-CodebaseAuditFollowUps` and `origin/ultrareview`: No `maintainer_docs/adr` citation remains in any `src/` file (only stale build-output XML under `bin/obj`, which is gitignored and regenerates from source). Full solution `dotnet build` and `dotnet test` (1189/1189) pass post-merge.
