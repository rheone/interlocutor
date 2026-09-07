# 02 — Rename `IVoidCommandHandler` to `IBareCommandHandler` in `Interlocutor.Generator`

**What to build:** every reference to the old handler names inside the source generator - well-known type name constants, handler/request-kind models, and the dispatch-wrapper emitter - is updated to match ticket 01's renamed types, so generated code continues to reference the real, current type names.

**Blocked by:** 01 (generated code must target the renamed runtime types, not the retired ones).

**Status:** done - implemented in `c5ee8b4`. (`dotnet test` for this project only became runnable once ticket 03 landed, since the test project transitively references `Interlocutor.DependencyInjection.Abstractions`; full-suite verification confirmed alongside ticket 03.)

- [ ] `WellKnownTypeNames` (or equivalent) references `IBareCommandHandler`/`BareCommandHandlerWrapper`/`IBareHandlerWrapper` by their new metadata names.
- [ ] `HandlerKind`/`RequestKind` models and `DispatchWrapperEmitter` are updated to match - any enum member or emitted identifier named after "Void" is renamed to "Bare."
- [ ] `InterlocutorDiagnostics` - any diagnostic message/description text referencing "void command"/"void commands" is updated to "bare command."
- [ ] Every test in `tests/Interlocutor.Generator.Tests` referencing the old names (`MissingHandlerDiagnosticTests`, `EjectEmissionTests`, `RequestDispatchSwitchEmissionTests`) is updated and continues to pass, including any generated-source golden-file/snapshot assertions that embed the old type name literally.
- [ ] A sample generated-source check confirms the generator emits `IBareCommandHandler`/`BareCommandHandlerWrapper` correctly for a bare command, not the retired names.
- [ ] `dotnet build` and `dotnet test` pass for `Interlocutor.Generator`; `dotnet csharpier .` produces no diff.
