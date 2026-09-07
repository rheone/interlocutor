# Naming Glossary

Project-specific naming decisions, maintained by `audit-naming` (`/audit-naming`).

## Exemptions

Names or patterns that look ambiguous/generic/vague but are deliberate — never re-flag these. Patterns support `*`/`?` wildcards matched against the bare identifier name.

| Pattern | Reason |
|---|---|
| `Result` | Bare, nested `Result` record types whose full meaning is disambiguated by an enclosing class name (e.g. `NotificationHandlerOrderPlanner.Result` in [NotificationHandlerOrderPlanner.cs](src/Interlocutor.Generator/Analysis/NotificationHandlerOrderPlanner.cs), `OpenGenericHandlerResolver.Result` in [OpenGenericHandlerResolver.cs](src/Interlocutor.Generator/Analysis/OpenGenericHandlerResolver.cs)) are exempt — internal-only, low-traffic generator-analysis types with no competing `Result` type nearby. Flagged 2026-09-07, kept as-is.

## Project glossary

Project-specific terms and concepts.

| Term | Definition |
|---|---|
