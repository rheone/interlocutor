# 02 - Convert DomainRepositoryQueryableExtensions to extension members

**What to build:** `Interlocutor.Persistence.Abstractions`'s query-composition helpers (filter, sort,
paging) are regrouped under a single C# 14 `extension<TEntity>(IQueryable<TEntity> query)` block instead
of three separate `this`-parameter extension methods each redeclaring the same receiver - the public
call-site behaviour (`query.ApplyFilter(...).ApplySort(...).ApplyPaging(...)`) is unchanged.

The one self-referencing XML doc `<see cref>` between these methods (the paging doc pointing at the
filter/sort methods) currently resolves as an unqualified sibling reference; once the target moves inside
an extension block, that form no longer binds, so it needs to become a fully class-qualified cref instead.

The package's `PublicAPI.Unshipped.txt` gets the new extension-block-shaped entries added alongside the
existing ones (additive, not a replacement - the classic-shape entries stay valid unless removed as part
of a later cleanup).

**Blocked by:** None - can start immediately

**Status:** ready-for-agent

- [ ] All three methods share one extension block on `IQueryable<TEntity>`, still in the same file
- [ ] The cross-method `<see cref>` is fully qualified and resolves (no CS1574)
- [ ] `PublicAPI.Unshipped.txt` updated to satisfy the analyzer with no RS0016/RS0017 warnings
- [ ] `dotnet build` and `dotnet test tests/Interlocutor.Persistence.Abstractions.Tests` are green
- [ ] `dotnet csharpier .` produces no diff on the touched file
