# Scripts

All scripts live under `scripts/`. Run any script with `-h` for full argument
details; they're also documented in each file's module docstring.

| Script | Mutates repo? | Purpose |
|---|---|---|
| `scripts/scan_repo.py` | No | Inventory a repo and diff it against `canonical-structure.json`. Returns missing entries, present entries, committed artifacts, casing issues, and uncategorized root files. |
| `scripts/apply_plan.py` | Only with `--execute` | Execute a reviewed JSON plan (bootstrap, refactor, or fill-gaps). Dry-run by default -- nothing writes without `--execute`. |
| `scripts/audit_paths.py` | Only with `--apply` | Find and optionally fix broken relative path references across `.csproj`, `.slnx`, `nuget.config`, workflows, shell scripts, and docs config. Resolves paths relative to the referencing file (not the repo root). |

## Typical invocation

```bash
# Scan (read-only)
python3 scripts/scan_repo.py . --library-name MyLib --groups benchmarks,smoketests,husky

# Apply a plan (dry-run first, always)
python3 scripts/apply_plan.py . plan.json --vars-file vars.json
python3 scripts/apply_plan.py . plan.json --vars-file vars.json --execute

# Audit paths after a refactor
python3 scripts/audit_paths.py . --rewrite-map moves.json
python3 scripts/audit_paths.py . --rewrite-map moves.json --apply
```
