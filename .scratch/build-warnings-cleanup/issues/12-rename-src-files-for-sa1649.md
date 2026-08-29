# 12 - Rename 2 src/ files to match their type names (SA1649)

**What to build:** `src/Interlocutor.DependencyInjection.Autofac/ContainerBuilderExtensions.cs` declares `InterlocutorAutofacExtensions`, and `src/Interlocutor.DependencyInjection.Microsoft/ServiceCollectionExtensions.cs` declares `InterlocutorServiceCollectionExtensions` - both are plain filename/type-name mismatches (single type per file, not a multi-type case), unrelated to ticket 11. Rename each file to match its actual public type name.

**Blocked by:** None - can start immediately.

**Status:** ready-for-agent

- [ ] Files renamed to `InterlocutorAutofacExtensions.cs` / `InterlocutorServiceCollectionExtensions.cs` (via `git mv`, preserving history)
- [ ] No type, namespace, or public API signature changes - rename only
- [ ] `dotnet build` reports zero `SA1649` for `src/Interlocutor.DependencyInjection.Autofac` and `src/Interlocutor.DependencyInjection.Microsoft`
- [ ] `dotnet test` passes for both packages' test projects
