---
diagram: Event Modeling
slug: event-modeling
status: experimental
mermaid_version_introduced: "v11.15.0"
mermaid_version_verified: "11.16.1"
keyword: eventmodeling
source: https://mermaid.js.org/syntax/eventmodeling.html
last_verified: 2026-08-09
plugin_required: false
---

# Event Modeling

> **Status:** Experimental - introduced v11.15.0. Syntax and support may change; treat generated diagrams of this type as more likely to need adjustment than stable types.

## Overview
Event Modeling is a diagram type for narrating how information flows through a system over time - as a timeline of UI actions, commands, the events they produce, and the read models built from those events - rather than depicting static structure. Mermaid's implementation lays each timeline step out left-to-right in numbered "time frames," auto-inferring the relationship arrows between consecutive steps unless told otherwise. This is Mermaid's newest diagram type, added in v11.15.0.

## Best-fit uses
- Narrating a use case as a timeline of user action, command, resulting event(s), and the read model that surfaces the result
- Designing or documenting an event-sourced or CQRS-style workflow before implementation
- Showing how a UI screen, a backend command handler, and a projection/read model relate for one specific flow
- Facilitating an Event Modeling / Event Storming-style workshop output in a text-based, versionable format

## When NOT to use this
- General request/response or service-to-service call ordering without an event-sourcing angle - see `sequence.md` instead, which is purpose-built for that and far more stable.
- A static view of what data/entities exist rather than how they change over time - see `entity-relationship.md` or `class.md` instead.
- Any diagram where syntax stability matters more than expressiveness - this is Mermaid's newest and least battle-tested diagram type.

## Basic syntax
- Start keyword: `eventmodeling`.
- Each step is a time frame: `tf <number> <entity-type> <identifier>` (compact) or `timeframe <number> <entity-type> <identifier>` (relaxed) - both notations are interchangeable within the same diagram.
- Entity types, compact / relaxed pairs: `ui` (no relaxed alias) for a user-interface trigger, `pcr`/`processor` for an automated/background trigger, `cmd`/`command` for a command, `evt`/`event` for an event, `rmo`/`readmodel` for a read model.
- Time frame numbers must be unique per diagram and establish the left-to-right ordering; Mermaid infers a relationship arrow from each time frame to the next unless a reset frame intervenes.
- Reset frame: `rf` / `resetframe` breaks the automatic inference chain, letting a new, unrelated timeline segment start without an arrow from the previous step.
- Inline data: append `{ ... }` on the same line as a time frame to attach a short payload description, e.g. `tf 02 cmd AddItem {item id}`.
- Data blocks: for longer payloads, reference a block with wiki-link syntax `[[identifier]]` and define its contents separately, keeping the time frame line itself short.
- Namespaces: prefix an identifier with `Namespace.`, e.g. `Inventory.InventoryChanged` - each distinct Namespace + entity-type pair gets its own swimlane, letting you group related timelines visually.
- Multiple relations: when a step depends on more than one preceding step (e.g. a read model built from several events), chain the extra relations with the `->>` token instead of relying on the default single-predecessor inference.
- Typed data blocks: a data block's content can be tagged with a backtick-prefixed type hint (e.g. json, text, uri) to hint how it should be rendered.

## Simple example

<!-- mermaid-validate: skip reason="Event Modeling is experimental in v11.16.1; parser fails on examples that appear correct per mermaid.js.org documentation" -->
```mermaid
eventmodeling
01 ui CartUI {select item}
02 cmd AddItem {item id, quantity}
03 evt ItemAdded {item id, quantity, cart id}
```
A user interaction (`ui`) triggers a command (`cmd`), which produces an event (`evt`); Mermaid infers the two connecting arrows automatically because the time frame numbers run consecutively with no reset frame between them.

## Complex example

<!-- mermaid-validate: skip reason="Event Modeling is experimental in v11.16.1; parser fails on examples that appear correct per mermaid.js.org documentation" -->
```mermaid
eventmodeling
01 ui CartUI {customer selects item}
02 cmd AddItem {item id, quantity}
03 evt ItemAdded {item id, quantity, cart id}
04 rmo CartSummary {running total, item count}

rf 05

06 ui CheckoutUI {customer confirms order}
07 cmd PlaceOrder {cart id}
08 evt OrderPlaced {order id, cart id, total}

09 pcr Inventory.ReserveStock {triggered by OrderPlaced}
10 evt Inventory.StockReserved {order id, reserved items}

11 rmo OrderConfirmation {order id, total, reserved items}
11 ->> 08
11 ->> 10
```
The `rf 05` reset frame separates the "add to cart" timeline from the "checkout" timeline so step 06 doesn't get an inferred arrow from step 04. The `Inventory.` namespace prefix on steps 09–10 places the background reservation processor and its event in their own swimlane, separate from the checkout flow. `OrderConfirmation` is a read model built from two prior steps, so both relations are stated explicitly with `->>` rather than relying on single-predecessor inference.

## Escaping & special characters
- Inline payload descriptions in `{ ... }` are free text; keep them short and avoid embedding raw `{`/`}` characters, since the parser treats the first matching pair as the payload boundary.
- Identifiers used as namespace-qualified names (`Namespace.Entity`) use a literal period as the separator - avoid periods inside an identifier for any other purpose.
- Data-block references use double-square-bracket wiki-link syntax (`[[identifier]]`); keep the identifier alphanumeric to avoid ambiguity with Markdown's own link syntax when this diagram is embedded in a larger document.
- Inside a fenced ```mermaid block in Markdown, blank lines between time frame groups are cosmetic and safe - they do not need escaping and can be used freely to visually separate timeline segments.

## Common pitfalls
- Relying on default inference across a `rf`/`resetframe` boundary - inference is intentionally cut there, so a missing explicit relation will leave a step disconnected.
- Reusing a time frame number - numbers must be unique per diagram since they double as the ordering and the target of `->>` relations.
- Building a read model or event from multiple predecessors without adding the extra `->>` relations - the default inference only connects consecutive single-predecessor steps.
- Mixing compact and relaxed keyword forms inconsistently in a way that hurts readability - both parse fine, but pick one convention per diagram for a team's sanity.
- Treating this like a general sequence diagram - it models information flow over named time frames and swimlanes, not arbitrary message passing, so shoehorning unrelated interaction patterns into it produces an awkward result.

## Beta/experimental caveats
This is Mermaid's newest diagram type (v11.15.0+) and its docs note that the underlying grammar is developed in a separate external DSL project intended to eventually support multiple output targets (e.g. IDE tooling), which signals the syntax is still actively evolving outside the main Mermaid release cadence. Always tell the user this diagram type requires Mermaid v11.15.0 or later (this skill is pinned to 11.16.1, which satisfies that), and that both the compact/relaxed keyword set and the inference/namespace rules are more likely than stable diagram types to change in a future Mermaid release.

## Further reading
- https://mermaid.js.org/syntax/eventmodeling.html
