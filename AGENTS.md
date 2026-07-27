# AGENTS.md — OpenCode Configuration & Practices

## Models

| Role | Model | Cost |
|------|-------|------|
| Planning (default) | `opencode-go/deepseek-v4-pro` | Paid |
| Coding | `opencode-go/deepseek-v4-flash` | Paid |

## Agents

| Agent | Model | Mode | Use For |
|-------|-------|------|---------|
| `plan` | deepseek-v4-pro | primary (default) | Planning, architecture — sessions start here |
| `reviewer` | deepseek-v4-pro | subagent | Code review (read-only) |
| `build` | deepseek-v4-flash | primary | General implementation |
| `docs` | deepseek-v4-flash | subagent | Documentation |

## Common Practices

### Workflow
1. Read existing code and documentation to understand patterns before changing it
2. Check for existing helpers/utilities and applicable skills
3. Plan the minimal change needed
4. TDD-leaning: write or update tests with behavior changes; verify before claiming done

### Code Style
- Concise implementations; match existing project style and libraries
- Value added comments only for non-obvious logic
- Rich XML documentation on all members
- Documentation: minimal but useful — README covers what/install/use

### Git
- Conventional commits (`feat:`, `fix:`, `chore:`, ...), imperative mood, <72 char subject
- Never commit secrets or .env files; stage related changes together

### Safety
- Create backups/checkpoints before destructive changes

## Agent skills

### Issue tracker

Local markdown — issues live as files under `.scratch/<feature>/`. No PR triage. See `docs/agents/issue-tracker.md`.

### Triage labels

Default labels: `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context: one `CONTEXT.md` and one `docs/adr/` at the repo root. See `docs/agents/domain.md`.
