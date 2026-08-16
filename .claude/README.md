# Claude Code Configuration

This directory contains configuration for Claude Code in the Interlocutor project.

## Structure

```
.claude/
├── README.md                 # This file
├── settings.json            # Global project settings
├── settings.local.json      # Local overrides (git-ignored, personal)
└── skills/                  # Project-specific Claude Code skills
```

## Setup

The development container automatically configures Claude Code when it starts. It:

1. Creates runtime directories in `/home/vscode/.claude-runtime`
2. Syncs Windows user-level settings (from `~/.claude` on Windows host)
3. Makes project-local settings discoverable
4. Links agents configuration from `.agents/`

## Skills Discovery

Claude Code searches for custom skills in this order:

1. `./.claude/skills` - Project-specific skills (this repo)
2. `./.agents/skills` - Scoped agent skills (this repo)
3. `~/.claude/skills` - User-level skills (synced from Windows host)
4. Built-in skills - Anthropic's global skill library

To reload skills after adding new ones:
```bash
# In Claude Code CLI or the extension
/reload-skills
```

## Configuration Files

### settings.json
Global project settings that apply to all users:
- Skill search paths
- Build/test/format commands
- Code style conventions
- Documentation context

### settings.local.json
Personal overrides (git-ignored):
- Permission allowlists
- Personal keyboard bindings
- IDE preferences
- Workspace-specific overrides

Edit via:
```bash
/config
```

## Environment Variables

These are set in the devcontainer and available to Claude Code:

| Variable | Value | Purpose |
|----------|-------|---------|
| `CLAUDE_RUNTIME_DIR` | `/home/vscode/.claude-runtime` | Runtime caches, worktrees |
| `CLAUDE_USER_CONFIG` | `/home/vscode/.claude` | User-level settings |
| `CLAUDE_PROJECT_CONFIG` | `/workspaces/interlocutor/.claude` | Project settings |
| `CLAUDE_AGENTS_CONFIG` | `/workspaces/interlocutor/.agents` | Agent definitions |

## Project Skills

Custom skills for Interlocutor development:

- **`interlocutor:code-review`** - Standards + Spec review (Standards enforcement, PR compliance)
- **`codebase-design`** - Deep module design patterns
- **`csharp-docs-and-comments`** - XML doc and comment improvements
- **`grilling`** - Stress-test ideas and decisions
- **`mermaid-diagram-generator`** - Generate diagrams (flowcharts, sequences, ERD, etc.)
- **`research`** - Topic research against primary sources
- **`resolving-merge-conflicts`** - Merge/rebase conflict resolution
- **`tdd`** - Test-driven development workflows
- **`writing-for-agents`** - Agent skill and documentation writing

Use them with `/skillname` or by naming them in requests.

## IDE Integration (VSCode)

The devcontainer includes Claude Code VSCode extension settings:

```json
{
  "claude.codeEditor.enabled": true,
  "claude.codeEditor.defaultDarkTheme": "Default Dark",
  "claude.codeEditor.defaultLightTheme": "Default Light"
}
```

**Access skills in VSCode:**
1. Open Command Palette: `Ctrl+Shift+P` (Windows/Linux) or `Cmd+Shift+P` (Mac)
2. Type "Claude" to see available commands
3. Most custom skills appear in contextual suggestions

## Common Workflows

### Add a Skill

1. Create `.claude/skills/my-skill/` directory
2. Write `SKILL.md` following [the CLAUDE.md writing guide](../CLAUDE.md)
3. Test with `/reload-skills` in Claude Code
4. Commit to git

### Override a Setting

```bash
# Edit personal settings (git-ignored)
/config
```

### Debug Claude Code

Set environment variables in the container:
```bash
export CLAUDE_DEBUG=1
export CLAUDE_TRACE=1
```

## References

- [CLAUDE.md](../CLAUDE.md) - Project documentation for Claude Code
- [AGENTS.md](../AGENTS.md) - Agent and skill writing conventions
- [CONTEXT.md](../CONTEXT.md) - Domain vocabulary and design decisions
- [Claude Code CLI Reference](https://claude.ai/code/docs)
- [Skill Writing Guide](../AGENTS.md)
