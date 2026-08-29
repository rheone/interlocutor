# 12 - Rename 2 src/ files to match their type names (SA1649)

**What to build:** `src/Interlocutor.DependencyInjection.Autofac/ContainerBuilderExtensions.cs` declares `InterlocutorAutofacExtensions`, and `src/Interlocutor.DependencyInjection.Microsoft/ServiceCollectionExtensions.cs` declares `InterlocutorServiceCollectionExtensions` - both are plain filename/type-name mismatches (single type per file, not a multi-type case), unrelated to ticket 11. Rename each file to match its actual public type name.

**Blocked by:** None - can start immediately.

**Status:** done

- [x] Files renamed to `InterlocutorAutofacExtensions.cs` / `InterlocutorServiceCollectionExtensions.cs` (via `git mv`, preserving history)
- [x] No type, namespace, or public API signature changes - rename only
- [x] `dotnet build` reports zero `SA1649` for `src/Interlocutor.DependencyInjection.Autofac` and `src/Interlocutor.DependencyInjection.Microsoft`
- [x] `dotnet test` passes for both packages' test projects

## Comments

Already implemented in commit `56141a9` (via `git mv`, confirmed by `git log --follow` on both files showing continuous history through the rename). Verified: `src/Interlocutor.DependencyInjection.Autofac/InterlocutorAutofacExtensions.cs` and `src/Interlocutor.DependencyInjection.Microsoft/InterlocutorServiceCollectionExtensions.cs` exist with matching type names; no type/namespace/API signature changed. Zero `SA1649` warnings for either package in a full rebuild; both packages' test projects pass.
