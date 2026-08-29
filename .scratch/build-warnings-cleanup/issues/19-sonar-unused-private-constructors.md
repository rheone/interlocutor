# 19 - S1144: remove or justify unused private constructors on test-only types

**What to build:** SonarAnalyzer's S1144 ("Remove unused constructor of private type") fires roughly thirty times, concentrated in `tests/Interlocutor.DependencyInjection.Autofac.Tests`, `tests/Interlocutor.DependencyInjection.Microsoft.Tests`, `tests/Interlocutor.DependencyInjection.Abstractions.Tests`, and smaller counts in `tests/Interlocutor.Persistence.EntityFrameworkCore.Tests`, `tests/Interlocutor.Persistence.NHibernate.Tests`, `tests/Interlocutor.Generator.Tests`, and `tests/Interlocutor.DependencyInjection.Microsoft.EntityFrameworkCore.Tests` (re-run `dotnet build -t:Rebuild` after `SonarAnalyzer.CSharp` lands, per ticket 15, for the current list). Each is a private nested type (record/class) used only as a marker/fixture shape for reflection-based scanning or generic-constraint tests, where the constructor itself is never explicitly called - verify case by case whether the constructor is genuinely dead (safe to remove, letting the compiler synthesize a default one if still needed) or is deliberately shaped a particular way for what the test is proving (e.g. a specific arity or visibility), in which case suppress with a recorded reason instead of removing it blind.

**Blocked by:** None - can start immediately.

**Status:** done

- [x] Every flagged constructor is either removed (verified genuinely unused, not needed for the test's own shape) or kept with an inline comment recording why it must stay exactly as declared
- [x] `dotnet build` reports zero `S1144` warnings
- [x] `dotnet test` passes for every affected test project

## Comments

S1144 covers more than constructors - it fired on private classes, a private field, and a private property too. Verified each flagged site case by case: ~26 are handler/notification-subscriber classes resolved only via `AddInterlocutor()`'s assembly-scanning DI registration (confirmed several sibling classes in the same files are *not* flagged because they're referenced via `typeof()` elsewhere, which is exactly the DI-vs-static-visibility split this ticket anticipated) - kept, with a `#pragma warning disable S1144` and a one-line reason at each site or wrapped block. `Widget.Id` (3 EF Core test files) is never read directly but is required by EF Core's key-by-convention - kept, same treatment. `GeneratorTestHarness.ResolvingHandlerRegistered` was already documented as deliberately unused (a module-initializer substitute) - extended its existing `RCS1213` pragma to cover `S1144` too. `RealSessionFixture.WidgetMap`'s constructor is invoked only by FluentNHibernate's mapping-assembly scan - same DI-invisible-to-static-analysis shape, kept with a reason.
