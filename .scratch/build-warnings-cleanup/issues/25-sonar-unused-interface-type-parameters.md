# 25 - S2326: interface type parameters that go unused in the interface's own member signatures

**What to build:** SonarAnalyzer's S2326 ("'<T>' is not used in the interface") fires on `src/Interlocutor.Abstractions/Commands/ICommand.cs(15,27)` (and other arities in the same family, per the ticket 13 precedent of same-name/different-arity `ICommand` declarations) plus several occurrences in `tests/Interlocutor.DependencyInjection.Abstractions.Tests`. This is very likely a deliberate marker-generic pattern - `ICommand<TResponse>` uses `TResponse` purely to let the mediator's generic dispatch machinery select the right handler/pipeline at compile time, not because any interface member's signature needs it directly - so read each flagged declaration's actual role before deciding: suppress with a comment recording the marker-generic rationale where that's true, or add real covariant/contravariant use if a genuine gap turns up.

**Blocked by:** None - can start immediately.

**Status:** done

- [x] Every flagged interface's unused type parameter is confirmed as a deliberate dispatch/discovery marker (suppressed with that reason recorded) or fixed if a genuine unused-parameter bug turns up
- [x] `dotnet build` reports zero `S2326` warnings
- [x] `dotnet test` passes solution-wide, and `tests/Interlocutor.Architecture.Tests` still passes (this touches core mediator contracts)

## Comments

Confirmed all 5 flagged type parameters are the deliberate marker-generic pattern this ticket anticipated: `ICommand<TResponse>`/`IQuery<TResponse>`'s `TResponse` exists purely so the mediator's generic `Send<TResponse>` overload can select the right handler/pipeline at compile time, and the two test-file interfaces/records (`GenericQuery<T>`, `ISampleOpenGenericService<T1,T2>`) exist purely to exercise open-generic handler discovery and registration-report formatting. Suppressed each with that reasoning recorded - no genuine unused-parameter bug found. `Interlocutor.Architecture.Tests` still passes.
