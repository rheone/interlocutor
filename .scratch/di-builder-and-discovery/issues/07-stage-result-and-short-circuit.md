## Labels

ready-for-agent

## What to build

Make the lightweight before/after hook genuinely useful: let a before-hook decline a request and return an outcome without the handler running, and guarantee the after-hooks unwind correctly when it does.

Today a stage's before-hook returns nothing, so short-circuiting requires writing a full pipeline behavior. And the after-hook contract is documented as running even on failure, but does not - if a before-hook throws, stages that already ran never receive their after-hook, so whatever they set up is never torn down.

**The signalling shape is forced, not stylistic.** Outcomes are value-type unions, so a nullable response cannot distinguish "did not short-circuit" from "short-circuited with the default value" - the success case is a field-less struct, making those the same bit pattern. Use a dedicated two-state result. This shape came out of the design discussion and encodes the decision more precisely than prose:

```csharp
public readonly struct StageResult<TResponse>
{
    public bool ShortCircuited { get; }
    public TResponse Response { get; }   // meaningful only when ShortCircuited
}
// default(StageResult<T>) is Continue - consistent with the
// library's existing "safe at default value" convention.
```

Do **not** change the outcome union types to accommodate this. Value-type unions are correct; the ambiguity lives only in the stage signature.

Semantics to implement:

- On short-circuit, remaining before-hooks are skipped and the handler is skipped.
- After-hooks run in reverse order for exactly the stages whose before-hook ran, including the stage that short-circuited, each receiving the short-circuit response.
- Behaviors outside the stage host observe an ordinary response, so a short-circuited request is still logged and audited.
- After-hooks do not run when the handler throws.
- An exception thrown by an after-hook is caught, logged at error level with the stage and request type, and execution continues. It is never silently discarded. An opt-in switch escalates to rethrow.

Both hooks are no-ops by default via an abstract base class. Stages also register as delegates, with an overload receiving the service provider so lambda-form stages can use services.

The stage host is an ordinary entry in the behavior list - the developer positions it, at any depth, and may register it more than once. It is explicitly **not** pinned relative to the transaction behavior; a transaction is only one of many things a developer might wrap.

The request type parameter becomes contravariant. The response type parameter cannot, because it appears in the before-hook's return.

## Acceptance criteria

- [ ] A before-hook can short-circuit with an outcome and the handler does not run
- [ ] Short-circuiting with a default-valued response is distinguishable from not short-circuiting
- [ ] After-hooks unwind in reverse for exactly the stages whose before-hook ran, including the short-circuiter
- [ ] Behaviors outside the stage host see a short-circuit as an ordinary response
- [ ] After-hooks do not run when the handler throws
- [ ] An after-hook exception is logged with stage and request type and does not stop execution; the opt-in switch rethrows
- [ ] Stages register as types or delegates; the delegate form can receive services
- [ ] The stage host can be placed at any position and registered more than once
- [ ] Outcome union types are unchanged
- [ ] Wrapper-level tests cover the unwind orderings; each is also asserted through the container seam

## Blocked by

03 - Builder API, alongside the legacy overloads
