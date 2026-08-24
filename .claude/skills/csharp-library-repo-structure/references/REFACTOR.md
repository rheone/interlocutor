# Refactor an existing repo

Bring an existing repo in line with the canonical structure. This is
bootstrap's mirror image: diff reality against the manifest and close the gap,
including things the manifest doesn't want (committed artifacts, scratch files,
casing drift).

## 1. Scan before touching anything

```bash
python3 scripts/scan_repo.py <repo-root> --library-name <Name>
```

Read-only. Returns: `missing`, `committed_artifacts`, `casing_issues`, and
`uncategorized_root_files`. `--library-name` is optional; the script tries to
detect it from `*.slnx` or project folders.

## 2. Triage findings into a plan -- with the user, not for them

Map findings to plan actions:

- **committed_artifacts** → `gitignore_add` (the pattern) + `git_rm_cached`
  (the tracked path) -- keeps the file on disk, stops tracking it
- **casing_issues** on project-shaped folders → `git_mv` into the correct
  wrapper folder, preserving the project's own casing
- **missing** entries → `create_from_template` (or `run` for SDK-generated files)

**uncategorized_root_files** are different: ask the user what each one is. A
`PRD-Draft.md` root file might be exactly where they want it, or belong in
`docs/design/`, or be safe to delete.

## 3. Check the working tree is clean, then execute

```bash
python3 scripts/apply_plan.py <repo-root> <plan.json> --vars-file vars.json --execute --require-clean
```

`--require-clean` refuses if `git status --porcelain` isn't empty. Use
`git_mv` for every move (preserves file history for `git log --follow` and
`git blame`).

## 4. Fix path references the moves just broke

Build a rewrite map from your plan's `git_mv` actions and run:

```bash
python3 scripts/audit_paths.py <repo-root> --rewrite-map moves.json --apply
```

This resolves every path reference relative to the file that contains it (not
the repo root). Run without `--apply` first to see the count.

> `.slnx` files use `<Project Path="..."/>` elements. If a project moved,
> prefer `dotnet sln remove <old>` + `dotnet sln add <new>` over
> text-editing the `.slnx` directly.

## 5. Verify with the real toolchain

`dotnet build` from `src/` is the ground truth. Fix failures before declaring
the refactor complete. Static path-checking catches a lot, but not everything.
