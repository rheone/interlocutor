# Agents Configuration

This directory contains Claude agent definitions and custom skills for the Interlocutor project.

## Structure

```
.agents/
├── README.md               # This file
├── settings.json          # Agent configuration and discovery
└── skills/                # Custom skills organized by name
    ├── skill-name/
    │   ├── SKILL.md       # Skill definition and instructions
    │   ├── agents/
    │   │   └── openai.yaml   # (legacy) Agent provider config
    │   └── [references/]  # Supporting documentation
    └── ...
```

## About This Directory

The `.agents/` directory stores:

1. **Skills** — Custom Claude Code skills that extend functionality
2. **Agent definitions** — YAML files configuring how skills run
3. **Skill documentation** — Usage guides and reference material

Skills here are **scoped to the Interlocutor project** — they are automatically preferred over global skills of the same name.

## Available Skills

### Code Review & Standards
- **`code-review`** — Review against Standards and Spec
- **`codebase-design`** — Deep module design patterns
- **`csharp-docs-and-comments`** — XML doc and code comment improvements

### Development Workflows
- **`tdd`** — Test-driven development (red-green-refactor, integration tests)
- **`resolving-merge-conflicts`** — Merge/rebase conflict resolution
- **`grilling`** — Stress-test ideas and design decisions

### Documentation & Writing
- **`mermaid-diagram-generator`** — Flowcharts, sequences, ERD, state, C4, Gantt, etc.
- **`research`** — Investigate topics against primary sources
- **`writing-for-agents`** — Write or improve agent skills and documentation

### Other
- **`triage`** — Issue triage and classification
- **`wayfinder`** — Navigate the codebase
- **`implement`** — Implement features
- **`handoff`** — Hand off work to another agent
- **`to-spec`** — Ensure code matches specification
- **`to-tickets`** — Convert findings to ticket format

## Using Skills

### In Claude Code (CLI or VSCode)

```bash
# List available skills
list skills

# Use a skill directly
/skillname <prompt>

# Example
/tdd "Write tests for result-type composition"
/codebase-design "Design a new behavior pipeline interface"
```

### In VSCode Claude Code Extension

1. Open a file or selection
2. Press `Ctrl+Shift+P` and type "Claude: Skills"
3. Select a skill from the list
4. Enter your prompt

## Creating a New Skill

### 1. Create the Skill Directory

```bash
mkdir -p .agents/skills/my-skill/agents
cd .agents/skills/my-skill
```

### 2. Write SKILL.md

```markdown
# My Skill Name

Brief description of what this skill does.

## How to Use

- Use this skill when: [situations]
- It will: [outcomes]

## Example

```bash
/my-skill "your prompt here"
```

## What It Does

Detailed explanation of the skill's behavior and how it works.
```

See [AGENTS.md](../AGENTS.md) for detailed skill writing conventions.

### 3. Add Agent Configuration (Optional)

If your skill needs custom model or behavior, create `agents/openai.yaml`:

```yaml
name: "my-skill"
description: "Skill description"
model: "claude-opus-5"
instructions: |
  You are a specialized agent for [domain].
  [Additional system instructions]
```

### 4. Add Supporting Documentation (Optional)

Create `.md` files in `references/` for:
- Detailed guides (`GUIDE.md`)
- Reference material (`REFERENCE.md`)
- Examples (`EXAMPLES.md`)
- Domain knowledge (`DOMAIN.md`)

### 5. Test the Skill

```bash
# In Claude Code
/reload-skills
/my-skill "test prompt"
```

### 6. Commit

```bash
git add .agents/skills/my-skill/
git commit -m "feat: add my-skill"
```

## Skill Structure Reference

Minimal skill:
```
.agents/skills/my-skill/
└── SKILL.md
```

Complete skill:
```
.agents/skills/my-skill/
├── SKILL.md                    # Main skill definition
├── agents/
│   └── openai.yaml            # Model configuration
├── references/
│   ├── GUIDE.md               # Usage guide
│   ├── REFERENCE.md           # Technical reference
│   ├── EXAMPLES.md            # Example prompts
│   └── DOMAIN.md              # Domain terminology
└── tools/                      # Any helper scripts
    └── my-tool.js
```

## Configuration

See `settings.json` for:
- Skill search paths
- Default agent selection
- Skill-specific overrides
- Build/test/format commands

## Integration with Claude Code

Skills are automatically discovered and available:
1. **Project-scoped** — Appear as `interlocutor:skill-name`
2. **Instant reload** — Changes take effect immediately
3. **Documented** — Use `/skill-doctor` to validate

## References

- [AGENTS.md](../AGENTS.md) — Agent and skill writing standards
- [CLAUDE.md](../CLAUDE.md) — Project-level Claude Code guidance
- [CONTEXT.md](../CONTEXT.md) — Domain terminology and design decisions
- [Claude Code Docs](https://claude.ai/code/docs)
