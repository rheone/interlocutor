> **Status: superseded** — this issue predates the common-kernel migration.
> Reason: done; positional parameters are now PascalCase (type names differ post-migration).
> Superseded by the effort in `.scratch/remediation/`. Do not pick this up.

## Labels

ready-for-agent

## What to build

Rename all positional record parameters in `Interlocutor.CommonKernel` from camelCase to PascalCase to match the project's `.editorconfig` conventions and produce clean public API properties.

Types affected:

| Type | Current (camelCase) | Target (PascalCase) |
|---|---|---|
| `PagedResult<T>` | `items`, `page`, `pageSize`, `totalCount` | `Items`, `Page`, `PageSize`, `TotalCount` |
| `ValidationFailed` | `errors` | `Errors` |
| `NotFound` | `entityType`, `key` | `EntityType`, `Key` |
| `RuleViolation` | `code`, `message` | `Code`, `Message` |
| `Forbidden` | `reason` | `Reason` |
| `Conflict` | `code`, `message` | `Code`, `Message` |

Since these are positional record parameters, the change is mechanical:
1. Rename the parameter in the primary constructor
2. Update all call sites that use named arguments (`code:` → `Code:`)
3. Update all call sites that reference the properties (`result.Value.Code` etc.)
4. Update all test files that reference these properties or call these constructors

This is a public API breaking change, so it should be done early (before other slices that may add more call sites).

## Acceptance criteria

- [ ] All positional parameters listed above use PascalCase
- [ ] All call sites (source + tests) updated
- [ ] Build succeeds with zero warnings
- [ ] All existing tests still pass

## Blocked by

None — can start immediately
