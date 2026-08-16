---
diagram: Entity Relationship Diagram
slug: entity-relationship
status: experimental
mermaid_version_introduced: "long-standing (pre-v10)"
mermaid_version_verified: "11.16.1"
keyword: erDiagram
source: https://mermaid.js.org/syntax/entityRelationshipDiagram.html
last_verified: 2026-08-09
plugin_required: false
---

# Entity Relationship Diagram

> **Status:** Experimental - introduced long-standing (pre-v10). Syntax and support may change; treat generated diagrams of this type as more likely to need adjustment than stable types.

## Overview
An ER diagram maps the entities (tables, domain objects) in a system and the relationships between them, expressed with crow's-foot cardinality notation on each end of a connecting line. Entities can optionally list their attributes - including primary/foreign/unique key markers - turning the diagram into something close to a logical data model rather than just a relationship sketch.

## Best-fit uses
- Sketching a database schema or logical data model with tables and cardinalities
- Documenting how domain entities relate before or alongside implementing a data layer
- Communicating one-to-many / many-to-many relationships precisely, including optionality (zero vs. one)
- Reviewing a normalized schema design with a non-technical stakeholder

## When NOT to use this
- A quick, informal sketch of related concepts without needing precise cardinality - a `flowchart.md` graph or `class.md` diagram is lighter-weight.
- Modeling class inheritance, interfaces, or object-oriented structure - see `class.md` instead, which has relationship arrows suited to OOP rather than data cardinality.
- Runtime/sequence behavior between records or services - see `sequence.md` instead.

## Basic syntax
- Start keyword: `erDiagram`.
- Relationship statement: `EntityA <left-cardinality>--<right-cardinality> EntityB : label` (the `label` is required whenever a relationship line is drawn).
- Relationship line style: `--` identifying relationship (solid), `..` non-identifying relationship (dashed).
- Cardinality markers, one per side, combined outer+inner:
  - `|o` / `o|` - zero or one
  - `||` - exactly one
  - `}o` / `o{` - zero or more (many)
  - `}|` / `|{` - one or more (many)
- Entity names: unicode allowed; wrap in double quotes if the name contains spaces, e.g. `"Line Item"`.
- Attribute block: `EntityName { type name key "comment" ... }`, one attribute per line (or comma-separated), where `key` is an optional `PK`, `FK`, or `UK` (comma-separated if an attribute is more than one), and `type` can carry an optional `?` suffix to mark the attribute nullable (v11.16.0+).
- Aliases: `EntityName["Display Alias"]` shows an alternate label without changing the identifier used in relationship statements.
- Direction: `TB`, `BT`, `LR`, `RL`.
- Styling: `style entityId ...`, and `classDef`/`class` for reusable style classes, same as other Mermaid diagram types.

## Simple example
```mermaid
erDiagram
    CUSTOMER ||--o{ ORDER : places
    ORDER ||--|{ LINE-ITEM : contains
```
Reads as: exactly one customer relates to zero-or-more orders, and exactly one order relates to one-or-more line items. Cardinality is read from the far end of the line relative to the entity you start at.

## Complex example
```mermaid
erDiagram
    CUSTOMER {
        int id PK
        string name
        string email UK
    }
    ORDER {
        int id PK
        int customer_id FK
        date? shipped_at "null until fulfilled"
        string status
    }
    "LINE ITEM" {
        int id PK
        int order_id FK
        int product_id FK
        int quantity
    }
    PRODUCT {
        int id PK
        string sku UK
        decimal price
    }
    PAYMENT {
        int id PK
        int order_id FK
        decimal amount
    }

    CUSTOMER ||--o{ ORDER : places
    ORDER ||--|{ "LINE ITEM" : contains
    PRODUCT ||--o{ "LINE ITEM" : "ordered as"
    ORDER ||--o{ PAYMENT : "paid via"
    CUSTOMER ||--o{ PAYMENT : "billed to"
```
Attribute blocks turn each entity into a mini schema definition; `PK`/`FK`/`UK` markers document keys directly, and the `date?` optional-type suffix flags a nullable column without needing a separate comment.

## Escaping & special characters
- Any entity name, attribute name, or relationship label containing a space, punctuation, or reserved character must be double-quoted: `"LINE ITEM"`, `"ordered as"`.
- Attribute-level free-text comments go in trailing double quotes after the key marker: `string status "one of: pending, shipped"`.
- Inside a fenced ```mermaid block in Markdown, avoid an unescaped triple-backtick inside any quoted label; if unavoidable, widen the surrounding Markdown fence to four backticks.
- Basic markdown emphasis (e.g. `**bold**`) is supported inside quoted relationship labels and renders as formatted text, not literal asterisks.

## Common pitfalls
- Reversing the cardinality pair - the marker touching an entity describes the *other* entity's cardinality relative to it, which reads backwards from plain English the first few times.
- Mixing up `|o` (zero-or-one) with `o|` - the vertical bar and circle are ordered differently depending on which side of the relation they sit on; copy the marker pair verbatim rather than reconstructing it from memory.
- Forgetting the relationship label after the second entity - it is not optional once a relationship line is present.
- Omitting quotes around a multi-word entity name or label, which breaks parsing rather than degrading gracefully.
- Using `--` (identifying) when the relationship is actually optional/non-identifying (`..`), which changes the rendered line style and, in some ER conventions, the implied meaning.
- Assuming attribute blocks are required - they are optional, and a relationship-only diagram is valid and often clearer for a quick sketch.

## Beta/experimental caveats
The live Mermaid documentation for this diagram type does not carry an explicit "experimental" banner in its prose - ER diagrams have existed in Mermaid since before v10 and are broadly usable. This skill nonetheless tracks the diagram as experimental-tier for planning purposes, so mention to the user that cardinality-notation edge cases and attribute-block formatting (e.g. the `?` optional-type suffix, which only landed in v11.16.0) are newer additions worth double-checking against the currently pinned Mermaid version (11.16.1) before relying on them in a rendered diagram.

## Further reading
- https://mermaid.js.org/syntax/entityRelationshipDiagram.html
