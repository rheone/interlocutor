# 03 - Convert PersistenceContainerBuilderExtensions (Autofac) to extension members

**What to build:** `Interlocutor.DependencyInjection.Autofac`'s opt-in persistence registration helpers
(unit-of-work and repository registration) are regrouped under a single C# 14
`extension(ContainerBuilder builder) { }` block instead of two separate `this`-parameter extension
methods each redeclaring the same receiver. Public call-site behaviour is unchanged.

This file has no self-referencing crefs between the two methods, so no doc fix-up is needed beyond the
block conversion itself.

`PublicAPI.Unshipped.txt` gets the new extension-block-shaped entries added alongside the existing ones
(additive, not a replacement).

**Blocked by:** None - can start immediately

**Status:** ready-for-agent

- [ ] Both methods share one extension block on `ContainerBuilder`, still in the same file
- [ ] `PublicAPI.Unshipped.txt` updated to satisfy the analyzer with no RS0016/RS0017 warnings
- [ ] `dotnet build` and `dotnet test tests/Interlocutor.DependencyInjection.Autofac.Tests` are green
- [ ] `dotnet csharpier .` produces no diff on the touched file
