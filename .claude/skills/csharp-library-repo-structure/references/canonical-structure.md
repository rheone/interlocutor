# Canonical .NET library repo structure

This is the prose/rationale companion to `canonical-structure.json`, which is the
machine-readable source of truth scripts actually consume. If you change one,
change the other.

```
{Repo}/
├── README.md
├── LICENSE
├── SECURITY.md
├── CONTRIBUTING.md
├── CODE_OF_CONDUCT.md
├── CHANGELOG.md
├── HUMANS.md
│
├── AGENTS.md                          # [agent_files] canonical agent instructions
├── CLAUDE.md                          # [agent_files] thin pointer to AGENTS.md only
├── .claude/                           # [agent_files] gitignored, machine-local
├── .opencode/                         # [agent_files] gitignored, machine-local
├── .agents/                           # [agent_files] gitignored, machine-local
│
├── {Lib}.slnx                         # solution file at repo root
├── global.json                        # pinned SDK version
│
├── docs/
│   └── design/
│       ├── PRD.md
│       ├── ROADMAP.md
│       └── adr/
│           └── 0001-record-architecture-decisions.md
│
├── .github/
│   ├── dependabot.yml
│   ├── pull_request_template.md
│   ├── CODEOWNERS
│   ├── ISSUE_TEMPLATE/
│   │   ├── bug_report.md
│   │   └── feature_request.md
│   ├── codeql/
│   │   └── codeql-config.yml
│   └── workflows/
│       ├── build.yml
│       ├── codeql.yml
│       ├── publish.yml
│       └── dependency-review.yml
│
├── .husky/                            # [husky]
│   ├── pre-commit
│   └── task-runner.json
│
├── .pre-commit-config.yaml            # [pre_commit] codespell, markdownlint
├── dictionary.dic                     # [pre_commit] codespell custom words
│
├── .config/
│   └── dotnet-tools.json
│
├── .editorconfig
├── .gitattributes
├── .gitignore
├── .csharpierrc.json
├── stylecop.json
│
├── src/
│   ├── Directory.Build.props
│   ├── Directory.Build.targets
│   ├── Directory.Packages.props
│   ├── {Lib}/
│   │   ├── {Lib}.csproj
│   │   ├── packages.lock.json
│   │   └── ...production code...
│   ├── {Lib}.Abstractions/            # [multi_project] only if interfaces are
│   │   └── {Lib}.Abstractions.csproj  # split from implementation
│   ├── {Lib}.Tests/
│   │   ├── {Lib}.Tests.csproj
│   │   └── ...test code, 1:1 mirroring {Lib}...
│   └── {Lib}.Benchmarks/              # [benchmarks]
│       ├── {Lib}.Benchmarks.csproj
│       ├── Program.cs
│       ├── run-benchmarks.sh
│       └── run-benchmarks.ps1
│
└── smoketests/                        # [smoketests]
    └── {Lib}.SmokeTests/
        ├── README.md
        ├── nuget.config
        ├── run-smoke-tests.sh
        ├── run-smoke-tests.ps1
        ├── feed/                      # gitignored -- built .nupkg lands here
        └── ConsumerProj/
            └── ConsumerProj.csproj
```

## Naming and casing

- **Wrapper folders are lowercase**: `src/`, `docs/`, `smoketests/`. These are
  repo-management concerns, not .NET artifacts, and lowercase keeps them visually
  distinct from project folders at a glance.
- **Project folders are PascalCase and match their `.csproj`/assembly name
  exactly**: `src/{Lib}.Tests/{Lib}.Tests.csproj`. Never `src/tests/` -- that
  breaks the "find a project by its name" mental model and most IDEs assume
  folder-name == project-name anyway.
- This is the one rule most existing repos drift on (mixing
  `{Lib}.SmokeTests/` at root with lowercase `src/`), so it's worth treating
  as non-negotiable across projects you want to look "the same."

## Why generated files are `template: null`

`.slnx`, `.csproj`, and `packages.lock.json` are deliberately **not** stubbed
from static templates in this skill. Hand-authored XML for these drifts from
whatever the installed SDK actually expects (project SDK versions, GUIDs,
target framework monikers) and is a common source of "this won't open in
Visual Studio" bugs. Always generate these with the `dotnet` CLI
(`dotnet new classlib`, `dotnet new slnx`, `dotnet sln add`,
`dotnet restore --use-lock-file`) and only hand-edit narrow, well-understood
pieces afterward (e.g. adding a `<ProjectReference>`).

## What "optional" actually means

Every entry tagged with a `group` in `canonical-structure.json`
(`benchmarks`, `smoketests`, `husky`, `pre_commit`,
`agent_files`, `multi_project`) is opt-in. Don't stamp every group into every
repo "for consistency" -- a 200-line utility library doesn't need
BenchmarkDotNet, and not every internal tool needs a CODE_OF_CONDUCT aimed at
external contributors. Consistency means *the same shape when the same
components are present*, not *every component everywhere*.

## Never commit

See `never_commit_patterns` in the JSON manifest. The most common real-world
violations, in order of how often they show up in audits:
`bin/ obj/`, `.vs/`, `TestResults/`, `*.nupkg` under `smoketests/**/feed/`,
and stray crash dumps (`mono_crash.*.json`) from a debugging session that
never got cleaned up.

## Loose root files

Anything that lands at repo root and isn't in the manifest -- session logs,
handoff notes, `*.old.md` copies, half-finished PRD forks -- is a signal of
drift, not a new convention. Flag it; don't silently relocate or delete it.
Durable content has a real home: changelog entries go in `CHANGELOG.md`,
design history goes in `docs/design/`, and "what changed and why" already
lives in git history, so a `RELEASE-vX.md` or `*.old.md` sitting next to it is
almost always redundant.
