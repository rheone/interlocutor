# 04 - Convert PersistenceServiceCollectionExtensions (Microsoft DI) to extension members

**What to build:** `Interlocutor.DependencyInjection.Microsoft`'s opt-in persistence registration helpers
(unit-of-work and repository registration) are regrouped under a single C# 14
`extension(IServiceCollection services) { }` block instead of two separate `this`-parameter extension
methods each redeclaring the same receiver. Public call-site behaviour is unchanged.

This file has no self-referencing crefs between the two methods, so no doc fix-up is needed beyond the
block conversion itself.

`PublicAPI.Unshipped.txt` gets the new extension-block-shaped entries added alongside the existing ones
(additive, not a replacement).

**Blocked by:** None - can start immediately

**Status:** done

- [x] Both methods share one extension block on `IServiceCollection`, still in the same file
- [x] `PublicAPI.Unshipped.txt` updated to satisfy the analyzer with no RS0016/RS0017 warnings
- [x] `dotnet build` and `dotnet test tests/Interlocutor.DependencyInjection.Microsoft.Tests` are green
- [x] `dotnet csharpier .` produces no diff on the touched file

## Comments

Mirrors ticket 03: each method's `<param name="services">` doc tag was dropped, since `services` is now the extension block's receiver. `dotnet build`, `dotnet test tests/Interlocutor.DependencyInjection.Microsoft.Tests`, and `dotnet csharpier check` all clean.
