# 01 — Build a custom-type registration builder for outcome-mapper dispatch

**What to build:** a standalone builder that lets a consumer register a mapping rule for their own custom failure case type, keyed by exact runtime type, with a later registration for the same type overriding an earlier one - the same builder → immutable-lookup shape `OutcomeClassifierBuilder`/`OutcomeCaseRegistryBuilder` already use elsewhere in this codebase. This ticket delivers the builder in isolation, fully testable on its own, with no wiring into `OutcomeMapperBase<TResult>` yet.

**Blocked by:** None — can start immediately.

**Status:** done - implemented in `cbc4980`.

- [ ] A new builder type in `Interlocutor.Abstractions.Pipeline.Mapping` accumulates `Type -> Func<TFailure, TResult>` registrations via a generic `RegisterType<TFailure>(Func<TFailure, TResult>)` method and a by-`Type`/`Delegate` overload, mirroring `OutcomeClassifierBuilder.RegisterType`'s two-overload shape.
- [ ] Registering a second mapping for a type already registered overrides the first (last-write-wins), matching `OutcomeClassifierBuilder`'s documented behavior.
- [ ] `Build()` produces an immutable, thread-safe lookup (`ImmutableDictionary`-backed, matching the existing registries' storage shape) that exposes a way to look up a registered mapping by exact `Type`, with a clear "not found" signal for an unregistered type.
- [ ] Unit tests cover: registering and looking up a mapping; overriding a previous registration for the same type; looking up an unregistered type returns "not found" rather than throwing.
- [ ] XML doc comments on every public member, in this codebase's existing documentation density and style.
- [ ] `dotnet build` and `dotnet test` pass for `Interlocutor.Abstractions`; `dotnet csharpier .` produces no diff.
