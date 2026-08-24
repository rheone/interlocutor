---
name: csharp-library-repo-structure
description: Bootstraps, audits, and refactors the file/folder layout of a .NET (C#) library repo meant for NuGet distribution.
Use whenever the user wants to scaffold a new .NET library repo (asks setup questions like project name, license, target framework);
wants an existing .NET repo's structure reviewed, audited, or "cleaned up" to match a consistent layout; mentions stray/committed
build artifacts (bin/, obj/, .vs/, TestResults/, crash dumps) cluttering a repo; is missing files or wants broken relative paths fixed
in .csproj, sln, slnx, nuget.config. Also trigger for "set up a new C# library like my other repos" or making multiple .NET repos
structurally consistent.
user-invocable: true
metadata:
  author: Robert Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# .NET library repo structure

Bootstraps new .NET library repos, audits/refactors existing ones against a
canonical layout, fills in missing standard files, and finds + fixes broken
path references.

## Entry points

| User wants | Workflow | Reference |
|---|---|---|
| A brand-new repo from scratch | Gather inputs, generate SDK artifacts, layer templates, verify | [BOOTSTRAP.md](references/BOOTSTRAP.md) |
| An existing repo brought in line | Scan, triage findings, execute plan, fix path references, verify | [REFACTOR.md](references/REFACTOR.md) |
| Just fill in what's absent | Same as refactor but only `missing` entries, no moves | [REFACTOR.md](references/REFACTOR.md) (skip the move/fix-steps) |

All three produce a JSON **plan** that `apply_plan.py` executes. This
indirection gives one place to review what will happen and one safety mechanism
(dry-run by default).

## Source of truth

- [`canonical-structure.json`](references/canonical-structure.json) -- machine-readable manifest consumed by scripts
- [`canonical-structure.md`](references/canonical-structure.md) -- same information as prose with rationale

If a structural choice conflicts with the manifest, discuss and update both
files together.

## Reference files

| File | Content |
|---|---|
| [BOOTSTRAP.md](references/BOOTSTRAP.md) | New project workflow |
| [REFACTOR.md](references/REFACTOR.md) | Existing repo workflow |
| [DOTNET-CLI.md](references/DOTNET-CLI.md) | dotnet CLI command cheat sheet |
| [GH-CLI.md](references/GH-CLI.md) | GitHub CLI for repo setup |
| [TEMPLATES.md](references/TEMPLATES.md) | Template authoring guide |
| [SCRIPTS.md](references/SCRIPTS.md) | Script reference |

## Cross-cutting rules

- **Dry-run first, always.** `apply_plan.py` defaults to dry-run; `--execute`
  requires explicit approval.
- **Idempotent by default.** `create_from_template` actions skip existing files
  (`skip_if_exists: true`). Re-running doesn't clobber customizations.
- **Templates are starting points, not final copy.** Placeholder emails and
  handles need user review before shipping.
- **Optional groups stay optional.** More components is not "more consistent."
  Consistency means the same shape when a component is present.
- **Never guess version numbers.** SDK versions, tool versions -- resolve from
  the actual environment or ask the user.

## Never commit

See `never_commit_patterns` in the manifest. Top violations in real audits:
`bin/ obj/`, `.vs/`, `TestResults/`, `*.nupkg`, and crash dumps.
