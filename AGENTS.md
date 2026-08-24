# AGENTS.md

When reporting information to me, be extremely concise and sacrifice grammar for sake of concision.

## Conventions

- Source files should contain only one top-level public type, and the file name should match the type name. The exception is simple record or struct types with the name name, which may include their custom member implementations (such as equality, formatting, or operator overrides) in the same file. For member ordering, partial-class splitting, generic-arity file naming, and region grouping, use the `csharp-code-organization` skill.

## Common Practices

### Workflow

1. Read relevant existing code and documentation to understand patterns before changing it
2. Check for existing helpers/utilities and applicable skills
3. Plan for the minimal change needed
4. TDD-leaning: write or update tests with behavior changes; verify before claiming done

### Code Style

- Concise implementations; match existing project style and libraries
- Value added comments only for non-obvious logic
- Rich XML documentation on all members public and private
- Documentation: minimal but useful - README covers what/install/use

### Attribution

- The user is the author of all work. Never claim authorship or credit in commits, docs, or code.

### Git

- Conventional commits (`feat:`, `fix:`, `chore:`, ...), imperative mood, <72 char subject
- Never commit secrets or .env files; stage related changes together

### Safety

- Create backups/checkpoints before destructive changes

## Agent skills

### Issue tracker

Local markdown - issues live as files under `.scratch/<feature>/`. No PR triage. See `docs/agents/issue-tracker.md`.

### Triage labels

Default labels: `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context: one `CONTEXT.md` and one `docs/adr/` at the repo root. See `docs/agents/domain.md`.

## Documentation Maintenance (ticket 35)

When a code change touches a `src/Interlocutor.*` package, consider whether the corresponding `documentation/` file needs updating:

| Package | Documentation file(s) |
|---------|------------------------|
| `Interlocutor.CommonKernel` | [messages-and-result-types.md](documentation/messages-and-result-types.md) |
| `Interlocutor.Abstractions` | [handler-selection-and-dispatch.md](documentation/handler-selection-and-dispatch.md), [pipeline-and-behaviors.md](documentation/pipeline-and-behaviors.md), [mediator-lifetimes-and-scope.md](documentation/mediator-lifetimes-and-scope.md) |
| `Interlocutor.DependencyInjection.Microsoft` / `.Autofac` | [builder-and-registration.md](documentation/builder-and-registration.md) |
| `Interlocutor.AspNetCore` | [aspnetcore-integration.md](documentation/aspnetcore-integration.md) |
| `Interlocutor.EntityFrameworkCore` / `.NHibernate` | [transactions-and-store-integration.md](documentation/transactions-and-store-integration.md) |
| `Interlocutor.FluentValidation` | [pipeline-and-behaviors.md](documentation/pipeline-and-behaviors.md) - ValidationBehavior section |
| `Interlocutor.Generator` | [builder-and-registration.md](documentation/builder-and-registration.md) - compilation discovery section |

**No CI enforcement** - this is advisory. If a code change might affect public API surface or developer-facing behavior, the person/agent making the change should review the mapped doc file(s) and update them if needed. Include doc updates in the same commit as code changes when they're part of a deliberate feature or API shift.
