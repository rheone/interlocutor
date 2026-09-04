# 05 - Collapse `InterlocutorPipelineBuilder`'s six `Add*Behavior` bodies onto a shared descriptor helper

**What to build:** Follow-up to the ticket 03 spike's "collapse" decision. `InterlocutorPipelineBuilder.cs` (`src/Interlocutor.DependencyInjection.Abstractions/`) has six `Add*Behavior` methods (`AddLoggingBehavior`, `AddAuditingBehavior`, `AddAuthorizationBehavior`, `AddCancellationBehavior`, `AddTimeoutBehavior`, `AddRateLimitedBehavior`) whose bodies are all `this.AddBehavior(typeof(X<,>)); this.TryAddDefault(...); [this.TryAddDefault(...);] return this;` - identical shape, differing only in the behavior type and zero-to-two default-dependency pairs.

Add a small private `BehaviorDescriptor` (behavior type + an array of `(Type ServiceType, Type ImplementationType)` default-dependency pairs) and one private `AddFromDescriptor(BehaviorDescriptor descriptor)` helper that performs exactly what the six bodies do today, in the same order. Each of the six public methods keeps its existing name, full XML doc comment (verbatim - including `AddLoggingBehavior`'s ADR-0009 remarks and the "no prescribed position" remark on `AddCancellationBehavior`/`AddTimeoutBehavior`/`AddRateLimitedBehavior`), and public signature, becoming a one-line expression body over a private `static readonly BehaviorDescriptor` field.

`AddValidationBehavior` and `AddUnitOfWorkBehavior` are out of scope - leave both exactly as they are (conditional reflective package-probing, skip-reporting, and, for `AddUnitOfWorkBehavior`, EF-Core-vs-NHibernate arity handling that isn't a `(type, defaults[])` shape).

**Blocked by:** Ticket 03 (spike) - decision recorded there.

**Status:** ready-for-agent

- [ ] `BehaviorDescriptor` and `AddFromDescriptor` exist as `private`/`internal` implementation detail - no new public API surface (`PublicAPI.Unshipped.txt` for `Interlocutor.DependencyInjection.Abstractions` untouched)
- [ ] Each of the six `Add*Behavior` methods keeps its existing XML doc comment verbatim and becomes a one-line expression body
- [ ] `AddValidationBehavior` and `AddUnitOfWorkBehavior` are unchanged
- [ ] Registration order and try-add-default precedence are identical to today for all six methods (existing `InterlocutorPipelineBuilderTests.cs` behavioral assertions pass with no modification)
- [ ] Every existing test in `tests/Interlocutor.DependencyInjection.Abstractions.Tests`, plus the Microsoft/Autofac DI integration suites that exercise `UseRecommendedChain`/individual `Add*Behavior` calls, passes with no modification

## Comments
