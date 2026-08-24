# csharp-library-repo-structure

Bootstraps, audits, and refactors the file/folder layout of a .NET (C#) library
repo meant for NuGet distribution.

## Structure

```
csharp-library-repo-structure/
├── SKILL.md                  # Entry point — dispatch hub linking to reference files
├── README.md                 # This file
├── references/               # Detailed workflow and reference documentation
│   ├── BOOTSTRAP.md          # New project workflow (gather inputs → verify)
│   ├── REFACTOR.md           # Existing repo workflow (scan → fix → verify)
│   ├── canonical-structure.json  # Machine-readable manifest (source of truth)
│   ├── canonical-structure.md    # Prose rationale and tree diagram
│   ├── DOTNET-CLI.md         # dotnet CLI command cheat sheet
│   ├── GH-CLI.md             # GitHub CLI for repo setup
│   ├── PACKAGES.md           # Recommended NuGet packages and .NET tools
│   ├── SCRIPTS.md            # Script reference (scan, apply, audit paths)
│   └── TEMPLATES.md          # Template authoring guide
├── assets/templates/         # 40 .tmpl stub files for scaffolding
├── scripts/                  # Python tools (read-only scanning + plan execution)
│   ├── scan_repo.py          # Inventory + diff against manifest (never mutates)
│   ├── apply_plan.py         # Execute reviewed JSON plans (dry-run by default)
│   └── audit_paths.py        # Find/fix broken path references after moves
└── examples/                 # Sample plan files and scan output
    ├── plan-bootstrap.json
    ├── plan-refactor.json
    ├── vars.json
    ├── moves.json
    └── report-scan.json
```

## Quick start

### Bootstrap a new repo

```bash
# 1. Scan the repo root (optional, since it's empty)
python3 scripts/scan_repo.py /path/to/repo

# 2. Generate a plan (the skill/agent does this interactively)
#    → gathers inputs: library name, license, TFMs, test framework, groups
#    → runs dotnet new slnx, dotnet new classlib, dotnet new xunit, etc.
#    → layers templates: .gitignore, .editorconfig, .csharpierrc.json, etc.

# 3. Review the dry-run output, then execute
python3 scripts/apply_plan.py /path/to/repo plan.json --vars-file vars.json --execute

# 4. Verify
dotnet build src/ && dotnet test src/
```

### Refactor an existing repo

```bash
# 1. Scan (read-only, never mutates)
python3 scripts/scan_repo.py /path/to/repo --library-name MyLib

# 2. Triage findings into a plan
#    → committed artifacts → gitignore_add + git_rm_cached
#    → casing issues → git_mv
#    → missing entries → create_from_template
#    → uncategorized root files → ask user

# 3. Execute (requires clean working tree)
python3 scripts/apply_plan.py /path/to/repo plan.json --vars-file vars.json \
  --execute --require-clean

# 4. Fix broken path references from moves
python3 scripts/audit_paths.py /path/to/repo --rewrite-map moves.json --apply

# 5. Verify
dotnet build src/
```

## Optional groups

| Group | What it adds |
|---|---|
| `benchmarks` | BenchmarkDotNet project under `src/` |
| `smoketests` | Package-consumption smoke tests under `smoketests/` |
| `husky` | `.husky/` pre-commit hooks (3-phase: style → analyzers → csharpier) |
| `pre_commit` | `.pre-commit-config.yaml` + `dictionary.dic` (codespell, markdownlint) |
| `agent_files` | `AGENTS.md` / `CLAUDE.md` agent-tooling conventions |
| `multi_project` | Split library into `LibraryName` + `LibraryName.Abstractions` |

## Canonical layout

```
{Repo}/
├── README.md, LICENSE, SECURITY.md, CONTRIBUTING.md, CODE_OF_CONDUCT.md
├── CHANGELOG.md, HUMANS.md
├── AGENTS.md, CLAUDE.md                           # [agent_files]
├── {Lib}.slnx, global.json
├── .config/dotnet-tools.json
├── .editorconfig, .gitattributes, .gitignore
├── .csharpierrc.json, stylecop.json
├── .pre-commit-config.yaml, dictionary.dic         # [pre_commit]
├── .husky/                                         # [husky]
├── .github/workflows/ (build, codeql, publish, dependency-review)
├── docs/design/ (PRD, ADRs, roadmap)
├── src/
│   ├── Directory.Build.{props,targets}, Directory.Packages.props
│   ├── {Lib}/, {Lib}.Tests/, {Lib}.Benchmarks/     # [benchmarks]
└── smoketests/{Lib}.SmokeTests/                    # [smoketests]
```

## Rules

- Dry-run first, always. `apply_plan.py` defaults to dry-run.
- Generated files (`.slnx`, `.csproj`, `packages.lock.json`) come from the `dotnet` CLI, never templates.
- Templates are starting points, not final copy — review placeholder emails and handles.
- Optional groups stay optional — don't stamp everything into every repo.
