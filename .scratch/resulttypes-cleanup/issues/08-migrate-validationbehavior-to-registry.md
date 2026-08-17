# 08 - Migrate ValidationBehavior to IOutcomeCaseRegistry

## Labels

ready-for-agent

**What to build:** Same migration as ticket 07, for `Interlocutor.FluentValidation.ValidationBehavior<TRequest, TResponse>`: replace its direct `UnionCaseFactory` call (and local `static readonly ConstructorInfo?` field) with `caseRegistry.Construct<TResponse, ValidationErrors>(...)` via an injected `IOutcomeCaseRegistry`.

**Blocked by:** 03 (IOutcomeCaseRegistry extension-point)

- [ ] `ValidationBehavior<TRequest, TResponse>` takes `IOutcomeCaseRegistry` via constructor injection
- [ ] The `ValidationErrors` short-circuit path calls `caseRegistry.Construct<TResponse, ValidationErrors>(...)` instead of calling `UnionCaseFactory` directly
- [ ] The local `static readonly ConstructorInfo?` field/manual reflection is removed
- [ ] Existing `ValidationBehavior` tests still pass unchanged in observed behavior
- [ ] A new test proves `ValidationBehavior` correctly short-circuits with `ValidationErrors` against a hand-rolled, non-union `IOperationOutcome` type registered with a custom `IOutcomeCaseRegistry`
- [ ] `dotnet test` for the FluentValidation test project passes
