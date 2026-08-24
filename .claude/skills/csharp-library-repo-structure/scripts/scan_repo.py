#!/usr/bin/env python3
"""
scan_repo.py -- Inventory a repo and diff it against canonical-structure.json.

Usage:
    python3 scan_repo.py <repo-root> [--library-name MyLib] [--groups benchmarks,smoketests,husky]

Output: JSON report on stdout with these keys:
    library_name         -- the name used for {{LibraryName}} substitution (detected or given)
    detected_groups       -- optional groups inferred as "present" from existing folders
    missing               -- canonical entries not found (respecting --groups / detected_groups)
    present                -- canonical entries found
    committed_artifacts    -- tracked-looking files matching never_commit_patterns
    casing_issues          -- wrapper/project folders that violate the casing rule
    uncategorized_root_files -- root-level files not in the manifest and not obviously
                                a canonical entry (session logs, *.old.*, handoff notes, etc.)

This script only *reports*. It never moves, deletes, or writes anything --
that's apply_plan.py's job, after a human has reviewed this output.
"""
import argparse
import fnmatch
import json
import re
import sys
from pathlib import Path

SCRIPT_DIR = Path(__file__).resolve().parent
MANIFEST_PATH = SCRIPT_DIR.parent / "references" / "canonical-structure.json"

IGNORE_DIRS = {".git"}

LOOSE_FILE_HINTS = re.compile(
    r"(\.old\.|-log\.|handoff|pilot|todo|scratch|batch-\d|release-v|notes\.)",
    re.IGNORECASE,
)


def load_manifest():
    with open(MANIFEST_PATH) as f:
        return json.load(f)


def detect_library_name(repo_root: Path) -> str | None:
    src = repo_root / "src"
    # Check repo root first (modern convention), then src/
    sln_candidates = (list(repo_root.glob("*.slnx")) + list(repo_root.glob("*.sln"))
                      + (list(src.glob("*.slnx")) + list(src.glob("*.sln")) if src.is_dir() else []))
    sln_candidates = [p for p in sln_candidates if p.is_file()]
    if sln_candidates:
        return sln_candidates[0].stem
    if not src.is_dir():
        return None
    # fall back to the first PascalCase dir under src/ that isn't a known suffix dir
    for child in sorted(src.iterdir()):
        if child.is_dir() and not any(
            child.name.endswith(suffix)
            for suffix in (".Tests", ".Benchmarks", ".Abstractions", ".SmokeTests")
        ):
            return child.name
    return None


def walk_files(repo_root: Path):
    for path in repo_root.rglob("*"):
        rel_parts = path.relative_to(repo_root).parts
        if any(part in IGNORE_DIRS for part in rel_parts):
            continue
        yield path


def substitute(path_template: str, library_name: str) -> str:
    return path_template.replace("{{LibraryName}}", library_name or "{{LibraryName}}")


def diff_against_manifest(repo_root: Path, manifest: dict, library_name: str, active_groups: set):
    missing, present = [], []
    for entry in manifest["entries"]:
        group = entry.get("group")
        if group and group not in active_groups:
            continue
        rel = substitute(entry["path"], library_name)
        target = repo_root / rel
        exists = target.exists() if not rel.endswith("/") else target.is_dir()
        (present if exists else missing).append({**entry, "resolved_path": rel})
    return missing, present


def find_committed_artifacts(repo_root: Path, manifest: dict):
    hits = []
    patterns = manifest["never_commit_patterns"]
    for path in walk_files(repo_root):
        rel = str(path.relative_to(repo_root))
        for pat in patterns:
            pat_norm = pat.rstrip("/")
            if fnmatch.fnmatch(rel, f"*{pat_norm}*") or fnmatch.fnmatch(path.name, pat_norm):
                hits.append(rel)
                break
    return sorted(set(hits))


def find_casing_issues(repo_root: Path, library_name: str):
    issues = []
    wrapper_expectations = {"src", "docs", "smoketests"}
    for child in repo_root.iterdir():
        if not child.is_dir() or child.name in IGNORE_DIRS:
            continue
        lower = child.name.lower()
        if lower in wrapper_expectations and child.name != lower:
            issues.append(
                {"path": child.name, "issue": f"wrapper folder should be lowercase '{lower}'"}
            )
        # a project-shaped name (contains the library name or a known suffix) sitting at root
        if library_name and library_name.lower() in lower and lower not in wrapper_expectations:
            issues.append(
                {
                    "path": child.name,
                    "issue": "looks like a project folder at repo root; project folders belong under src/ or smoketests/",
                }
            )
    src = repo_root / "src"
    if src.is_dir():
        for child in src.iterdir():
            if child.is_dir() and child.name[:1].islower():
                issues.append(
                    {"path": f"src/{child.name}", "issue": "project folders under src/ should be PascalCase"}
                )
    return issues


def find_uncategorized_root_files(repo_root: Path, manifest: dict, library_name: str):
    allowlisted = set()
    for entry in manifest["entries"]:
        rel = substitute(entry["path"], library_name)
        if "/" not in rel.rstrip("/"):
            allowlisted.add(rel)
    known_dotfiles = {".gitignore", ".gitattributes", ".editorconfig"}
    flagged = []
    for child in repo_root.iterdir():
        if child.is_dir() or child.name in IGNORE_DIRS:
            continue
        if child.name in allowlisted or child.name in known_dotfiles:
            continue
        flag_reason = "not in manifest"
        if LOOSE_FILE_HINTS.search(child.name):
            flag_reason = "looks like a scratch/session/handoff file"
        flagged.append({"path": child.name, "reason": flag_reason})
    return flagged


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("repo_root", type=Path)
    ap.add_argument("--library-name", default=None)
    ap.add_argument("--groups", default="", help="comma-separated optional groups to also expect, e.g. benchmarks,smoketests,husky,pre_commit,agent_files")
    args = ap.parse_args()

    repo_root = args.repo_root.resolve()
    manifest = load_manifest()
    library_name = args.library_name or detect_library_name(repo_root)

    active_groups = {g.strip() for g in args.groups.split(",") if g.strip()}
    # auto-detect groups already present so a refactor run doesn't flag e.g.
    # existing files as "optional and absent" when they're right there
    if (repo_root / ".husky").is_dir():
        active_groups.add("husky")
    if (repo_root / "AGENTS.md").exists() or (repo_root / "CLAUDE.md").exists():
        active_groups.add("agent_files")
    if list((repo_root / "src").glob("*.Benchmarks")) if (repo_root / "src").is_dir() else False:
        active_groups.add("benchmarks")
    if (repo_root / "smoketests").is_dir() or list(repo_root.glob("*SmokeTests")):
        active_groups.add("smoketests")
    if (repo_root / ".pre-commit-config.yaml").exists():
        active_groups.add("pre_commit")

    missing, present = diff_against_manifest(repo_root, manifest, library_name, active_groups)

    report = {
        "library_name": library_name,
        "active_groups": sorted(active_groups),
        "missing": missing,
        "present_count": len(present),
        "committed_artifacts": find_committed_artifacts(repo_root, manifest),
        "casing_issues": find_casing_issues(repo_root, library_name),
        "uncategorized_root_files": find_uncategorized_root_files(repo_root, manifest, library_name),
    }
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    sys.exit(main())
