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

**Status:** done

- [x] All three methods share one extension block on `IQueryable<TEntity>`, still in the same file
- [x] The cross-method `<see cref>` is fully qualified and resolves (no CS1574)
- [x] `PublicAPI.Unshipped.txt` updated to satisfy the analyzer with no RS0016/RS0017 warnings
- [x] `dotnet build` and `dotnet test tests/Interlocutor.Persistence.Abstractions.Tests` are green
- [x] `dotnet csharpier .` produces no diff on the touched file

## Comments

`ApplyPaging`'s doc comment previously carried a `<param name="query">` tag documenting the call-order note; since `query` is now the extension block's receiver rather than a method parameter, that note moved into the `<summary>` (as prose, still linking the two crefs) to avoid a CS1572/SA1612 mismatch. The three RS0016-flagged extension-block symbols (the block declaration plus the three methods) were added to `PublicAPI.Unshipped.txt` alongside the existing classic-shape entries. `dotnet build`, `dotnet test tests/Interlocutor.Persistence.Abstractions.Tests`, and `dotnet csharpier check` all clean.
