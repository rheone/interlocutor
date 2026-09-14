# Best Practices

Guidance for using Interlocutor well in your own application, as opposed to the per-topic reference docs that describe what the library does. This page is a starting point, not a finished checklist - it stays intentionally short and grows only as real usage across consuming applications settles on a pattern worth naming here. Where a full explanation already lives elsewhere in this directory or in the root [README.md](../README.md), this page links to it rather than restating it.

## Choose the narrowest outcome arity a handler can produce

Don't route every handler failure through a single generic `Failure`. Pick the specific [`Outcome<...>`](glossary.md#result-types) arity and case types that an operation can actually produce, so the compiler forces every caller to handle exactly those cases - no more, no less. See [Messages, Result Types & Variance § Choosing Arities](messages-and-result-types.md#choosing-arities) for the worked examples.

## Opt into pipeline behavior with marker interfaces

A request opts into a cross-cutting [pipeline behavior](glossary.md#pipeline) by implementing a [marker interface](glossary.md#marker-interfaces) (`IAuditableRequest`, `IRequiresPolicyRequest`, `ITransactionalRequest`, and the rest) - never by editing a central configuration list, and never by a behavior inspecting the request's concrete type. This keeps a request's opted-in behaviors visible on the request type itself. See [Messages, Result Types & Variance § Marker Interfaces](messages-and-result-types.md#marker-interfaces) for the full list and a worked example.

## Start from the recommended pipeline order

If you're composing `Logging`, `Validation`, and the store-specific unit-of-work behavior - `UseRecommendedChain()`'s own minimal default - or you've added `Auditing` and/or `Authorization` back in alongside them, start from the order the library documents - Logging → Auditing → Authorization → Validation → Transaction - rather than re-deriving it. See the [root README's Quick Start](../README.md#quick-start) for `UseRecommendedChain()` and [Pipeline & Behavior Composition § Why This Order](pipeline-and-behaviors.md#why-this-order) for the reasoning behind each position, and [§ Adding Auditing and Authorization Back](pipeline-and-behaviors.md#adding-auditing-and-authorization-back) for when either is worth adding. It's a documented recommendation, not a default the library imposes - compose a different order when your application has a reason to.

## More to come

This page will grow as more usage patterns settle across applications that consume Interlocutor. If you've found a practice worth generalizing, it belongs here.
