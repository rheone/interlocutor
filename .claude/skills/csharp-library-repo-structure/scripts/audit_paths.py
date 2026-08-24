#!/usr/bin/env python3
"""
audit_paths.py -- Find and optionally fix broken or suspicious path references
across a .NET repo: .csproj/.sln project references, Directory.Build.props
imports, nuget.config feeds, GitHub Actions workflow paths, shell/ps1 scripts,
and docs configuration files.

Report mode (default, always safe):
    python3 audit_paths.py <repo-root>

Fix mode (after a refactor moved things -- requires a mapping of old->new
repo-relative paths, typically produced from the same plan apply_plan.py just
executed):
    python3 audit_paths.py <repo-root> --rewrite-map moves.json --apply

moves.json: {"old/relative/path": "new/relative/path", ...}

Why this exists: a relative path that resolves correctly from the repo root
is frequently WRONG, because the reference is evaluated relative to the
*referencing file's own directory* (MSBuild, nuget.config, shell scripts),
not the repo root. This script always resolves relative to the file that
contains the reference, which is the actual semantics for every file type
it checks.
"""
import argparse
import json
import re
import sys
from dataclasses import dataclass, field
from pathlib import Path
from typing import Callable

IGNORE_DIRS = {".git", "bin", "obj", "node_modules", ".vs"}

# Flags suspicious absolute / machine-specific paths -- these are "direct"
# paths that will break for every other contributor and in CI.
SUSPICIOUS_ABSOLUTE = re.compile(
    r"""(?:[A-Za-z]:\\Users\\[^"'\s]+      # Windows user-profile path
        |/(?:home|Users)/[^"'\s]+          # POSIX user-profile path
        |/mnt/[^"'\s]+                     # container-mount path leaking into a committed file
       )""",
    re.VERBOSE,
)


@dataclass
class Finding:
    file: str
    raw_path: str
    resolved: str
    exists: bool
    kind: str
    line: int


@dataclass
class Handler:
    name: str
    glob: str
    patterns: list  # list of compiled regex with one capture group = the path

    def find(self, path: Path) -> list:
        results = []
        try:
            text = path.read_text(errors="ignore")
        except OSError:
            return results
        for lineno, line in enumerate(text.splitlines(), start=1):
            for pat in self.patterns:
                for m in pat.finditer(line):
                    raw = m.group(1)
                    if not raw or raw.startswith(("http://", "https://", "${{", "$(")):
                        continue
                    results.append((lineno, raw))
        return results


HANDLERS = [
    Handler("csproj/targets/props", "*.csproj", [
        re.compile(r'Include="([^"]*[\\/][^"]*)"'),
        re.compile(r'Import\s+Project="([^"]+)"'),
    ]),
    Handler("Directory.Build.props/.targets", "Directory.Build.*", [
        re.compile(r'Import\s+Project="([^"]+)"'),
        re.compile(r"Exists\('([^']+)'\)"),
    ]),
    Handler("sln", "*.sln", [
        re.compile(r'=\s*"[^"]+",\s*"([^"]+\.csproj)"'),
    ]),
    Handler("slnx", "*.slnx", [
        re.compile(r'<Project\s+Path="([^"]+)"'),
        re.compile(r'<File\s+Path="([^"]+)"'),
    ]),
    Handler("nuget.config", "nuget.config", [
        re.compile(r'<add\s+key="[^"]+"\s+value="(\.[^"]*)"'),
    ]),
    Handler("github workflow", "*.yml", [
        re.compile(r'working-directory:\s*([./][\S]*)'),
        re.compile(r'\bpath:\s*([./][\S]*)'),
        re.compile(r'uses:\s*(\./[\S]*)'),
    ]),
    Handler("shell script", "*.sh", [
        re.compile(r'(?:cd|cp|source|\.)\s+["\']?(\.\.?/[^\s"\']+)'),
    ]),
    Handler("powershell script", "*.ps1", [
        re.compile(r'(?:Set-Location|Copy-Item|Push-Location)\s+["\']?(\.\.?[\\/][^\s"\']+)'),
    ]),
    Handler("sphinx conf.py", "conf.py", [
        re.compile(r"abspath\(['\"](\.[^'\"]*)['\"]\)"),
    ]),
]


def iter_target_files(repo_root: Path):
    for handler in HANDLERS:
        for path in repo_root.rglob(handler.glob):
            if any(part in IGNORE_DIRS for part in path.relative_to(repo_root).parts):
                continue
            yield handler, path


def normalize(raw: str) -> str:
    return raw.replace("\\", "/")


def resolve(file_path: Path, raw: str, repo_root: Path) -> Path:
    candidate = (file_path.parent / normalize(raw)).resolve()
    return candidate


def scan(repo_root: Path):
    findings = []
    absolute_hits = []
    for handler, path in iter_target_files(repo_root):
        for lineno, raw in handler.find(path):
            resolved = resolve(path, raw, repo_root)
            exists = resolved.exists()
            try:
                resolved_rel = str(resolved.relative_to(repo_root))
            except ValueError:
                resolved_rel = str(resolved)  # escapes repo root entirely -- always worth flagging
                exists = False
            findings.append(Finding(
                file=str(path.relative_to(repo_root)),
                raw_path=raw,
                resolved=resolved_rel,
                exists=exists,
                kind=handler.name,
                line=lineno,
            ))
        text = path.read_text(errors="ignore")
        for m in SUSPICIOUS_ABSOLUTE.finditer(text):
            absolute_hits.append({"file": str(path.relative_to(repo_root)), "match": m.group(0)})
    return findings, absolute_hits


def apply_fixes(repo_root: Path, findings: list, rewrite_map: dict):
    """For each broken finding whose resolved path is a key in rewrite_map,
    rewrite the raw reference in its source file to point at the new location."""
    by_file = {}
    for f in findings:
        if f.exists or f.resolved not in rewrite_map:
            continue
        by_file.setdefault(f.file, []).append(f)

    changed_files = []
    for rel_file, items in by_file.items():
        file_path = repo_root / rel_file
        text = file_path.read_text()
        for f in items:
            new_target = repo_root / rewrite_map[f.resolved]
            new_rel = Path(
                _relpath(file_path.parent, new_target)
            ).as_posix()
            uses_backslash = "\\" in f.raw_path
            replacement = new_rel.replace("/", "\\") if uses_backslash else new_rel
            if f.raw_path in text:
                text = text.replace(f.raw_path, replacement)
        file_path.write_text(text)
        changed_files.append(rel_file)
    return changed_files


def _relpath(from_dir: Path, to_path: Path) -> str:
    import os
    return os.path.relpath(to_path, start=from_dir)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("repo_root", type=Path)
    ap.add_argument("--rewrite-map", type=Path, default=None)
    ap.add_argument("--apply", action="store_true", help="actually rewrite files (default is dry-run report only)")
    args = ap.parse_args()

    repo_root = args.repo_root.resolve()
    findings, absolute_hits = scan(repo_root)
    broken = [f for f in findings if not f.exists]

    report = {
        "checked_files": len({f.file for f in findings}),
        "total_references": len(findings),
        "broken": [f.__dict__ for f in broken],
        "suspicious_absolute_paths": absolute_hits,
    }

    if args.rewrite_map:
        rewrite_map = json.loads(args.rewrite_map.read_text())
        if args.apply:
            changed = apply_fixes(repo_root, findings, rewrite_map)
            report["fixed_files"] = changed
            # re-scan to show what's still broken after fixing
            findings2, _ = scan(repo_root)
            report["still_broken_after_fix"] = [f.__dict__ for f in findings2 if not f.exists]
        else:
            fixable = [f.resolved for f in broken if f.resolved in rewrite_map]
            report["would_fix_count"] = len(fixable)
            report["note"] = "Re-run with --apply to write these fixes."

    print(json.dumps(report, indent=2))
    return 1 if broken and not args.apply else 0


if __name__ == "__main__":
    sys.exit(main())
