# 03 — Rename `IVoidCommandHandler` to `IBareCommandHandler` in the DI packages

**What to build:** every reference to the old handler names inside `Interlocutor.DependencyInjection.Abstractions`, `.Microsoft`, and `.Autofac` - registration-time handler-kind discrimination, scanning, and registration reporting - is updated to match ticket 01's renamed types.

**Blocked by:** 01 (depends on `Interlocutor.Abstractions`'s renamed handler contracts). Independent of 02 - both depend only on 01.

**Status:** ready-for-agent

- [ ] `GeneratedHandlerKind`, `HandlerDiscovery`, `InterlocutorBuilder`, and `RegistrationProblemKind` in `Interlocutor.DependencyInjection.Abstractions` are updated wherever they reference the old names.
- [ ] `Interlocutor.DependencyInjection.Microsoft`'s `InterlocutorServiceCollectionExtensions` is updated to match.
- [ ] Every test in `tests/Interlocutor.DependencyInjection.Abstractions.Tests` (`HandlerDiscoveryTests`), `tests/Interlocutor.DependencyInjection.Autofac.Tests` (`GeneratedDispatchSwitchIntegrationTests`), and `tests/Interlocutor.DependencyInjection.Microsoft.Tests` (`GeneratedDispatchSwitchIntegrationTests`) is updated to the new names and continues to pass.
- [ ] A registration-scanning integration test confirms a `IBareCommandHandler<,>` implementation is discovered and registered correctly by both the Microsoft and Autofac packages.
- [ ] `PublicAPI.Unshipped.txt` (`Interlocutor.DependencyInjection.Abstractions`) updated for any renamed public member.
- [ ] `dotnet build` and `dotnet test` pass for all three DI packages; `dotnet csharpier .` produces no diff.
