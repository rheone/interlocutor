#!/usr/bin/env python3
"""
apply_plan.py -- Execute a reviewed JSON migration/bootstrap plan.

Dry-run by default. Nothing is written, moved, or deleted unless --execute
is passed. This is the ONLY script in this skill that mutates the repo --
scan_repo.py and audit_paths.py (without --apply) only report.

Usage:
    python3 apply_plan.py <repo-root> <plan.json> [--vars-file vars.json] [--execute] [--require-clean]

Plan = JSON list of actions, each one of:
    {"action": "mkdir", "path": "rel/dir"}
    {"action": "git_mv", "from": "old/rel/path", "to": "new/rel/path"}
    {"action": "gitignore_add", "pattern": "TestResults/"}
    {"action": "git_rm_cached", "path": "rel/path"}            # untrack without deleting from disk
    {"action": "create_from_template", "template": "README.md.tmpl", "dest": "README.md", "vars": {...}, "skip_if_exists": true}
    {"action": "create_file", "dest": "rel/path", "content": "...", "skip_if_exists": true}
    {"action": "run", "command": "dotnet new classlib -n {{LibraryName}} -o src/{{LibraryName}}"}

Every action accepts an optional "reason" key (ignored by the executor,
shown in --execute output) -- always fill it in when building a plan so the
human reviewing it understands *why* before approving --execute.
"""
import argparse
import json
import shlex
import subprocess
import sys
from pathlib import Path

SCRIPT_DIR = Path(__file__).resolve().parent
TEMPLATES_DIR = SCRIPT_DIR.parent / "assets" / "templates"


def substitute(text: str, variables: dict) -> str:
    for key, value in variables.items():
        text = text.replace("{{" + key + "}}", str(value))
    return text


def git(repo_root: Path, *args) -> subprocess.CompletedProcess:
    return subprocess.run(["git", "-C", str(repo_root), *args], capture_output=True, text=True)


def check_clean(repo_root: Path) -> bool:
    result = git(repo_root, "status", "--porcelain")
    return result.returncode == 0 and result.stdout.strip() == ""


def do_mkdir(repo_root, action, execute, log):
    target = repo_root / substitute(action["path"], action.get("vars", {}))
    log.append(f"mkdir -p {target.relative_to(repo_root)}")
    if execute:
        target.mkdir(parents=True, exist_ok=True)


def do_git_mv(repo_root, action, execute, log, errors):
    src = repo_root / action["from"]
    dst = repo_root / action["to"]
    log.append(f"git mv {action['from']} -> {action['to']}")
    if not execute:
        return
    if not src.exists():
        errors.append(f"git_mv source missing: {action['from']}")
        return
    dst.parent.mkdir(parents=True, exist_ok=True)
    result = git(repo_root, "mv", action["from"], action["to"])
    if result.returncode != 0:
        errors.append(f"git mv failed for {action['from']} -> {action['to']}: {result.stderr.strip()}")


def do_gitignore_add(repo_root, action, execute, log):
    pattern = action["pattern"]
    log.append(f"gitignore += {pattern}")
    if not execute:
        return
    gi = repo_root / ".gitignore"
    existing_text = gi.read_text() if gi.exists() else ""
    existing_lines = existing_text.splitlines()
    if pattern not in existing_lines:
        if existing_text and not existing_text.endswith("\n"):
            existing_text += "\n"
        gi.write_text(existing_text + pattern + "\n")


def do_git_rm_cached(repo_root, action, execute, log):
    log.append(f"git rm -r --cached {action['path']}")
    if execute:
        git(repo_root, "rm", "-r", "--cached", "--ignore-unmatch", action["path"])


def do_create_from_template(repo_root, action, execute, log, errors, global_vars):
    dest = repo_root / substitute(action["dest"], global_vars)
    log.append(f"create (from template {action['template']}) -> {dest.relative_to(repo_root)}")
    if action.get("skip_if_exists", True) and dest.exists():
        log[-1] += "  [skipped: already exists]"
        return
    if not execute:
        return
    template_path = TEMPLATES_DIR / action["template"]
    if not template_path.exists():
        errors.append(f"template not found: {action['template']}")
        return
    variables = {**global_vars, **action.get("vars", {})}
    content = substitute(template_path.read_text(), variables)
    dest.parent.mkdir(parents=True, exist_ok=True)
    dest.write_text(content)


def do_create_file(repo_root, action, execute, log, global_vars):
    dest = repo_root / substitute(action["dest"], global_vars)
    log.append(f"create -> {dest.relative_to(repo_root)}")
    if action.get("skip_if_exists", True) and dest.exists():
        log[-1] += "  [skipped: already exists]"
        return
    if not execute:
        return
    content = substitute(action.get("content", ""), {**global_vars, **action.get("vars", {})})
    dest.parent.mkdir(parents=True, exist_ok=True)
    dest.write_text(content)


def do_run(repo_root, action, execute, log, errors, global_vars):
    command = substitute(action["command"], global_vars)
    cwd = repo_root / action.get("cwd", ".")
    log.append(f"$ ({cwd.relative_to(repo_root) or '.'}) {command}")
    if not execute:
        return
    parts = shlex.split(command)
    result = subprocess.run(parts, cwd=cwd, capture_output=True, text=True)
    if result.returncode != 0:
        errors.append(f"command failed [{command}]: {result.stderr.strip()[:500]}")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("repo_root", type=Path)
    ap.add_argument("plan_file", type=Path)
    ap.add_argument("--vars-file", type=Path, default=None)
    ap.add_argument("--execute", action="store_true")
    ap.add_argument("--require-clean", action="store_true", help="refuse to execute if `git status` is dirty")
    args = ap.parse_args()

    repo_root = args.repo_root.resolve()
    plan = json.loads(args.plan_file.read_text())
    global_vars = json.loads(args.vars_file.read_text()) if args.vars_file else {}

    if args.execute and args.require_clean and not check_clean(repo_root):
        print(json.dumps({"error": "refusing to execute: working tree is not clean (git status --porcelain is non-empty). Commit or stash first, or omit --require-clean if this is intentional."}))
        return 1

    log, errors = [], []
    for action in plan:
        kind = action["action"]
        if kind == "mkdir":
            do_mkdir(repo_root, action, args.execute, log)
        elif kind == "git_mv":
            do_git_mv(repo_root, action, args.execute, log, errors)
        elif kind == "gitignore_add":
            do_gitignore_add(repo_root, action, args.execute, log)
        elif kind == "git_rm_cached":
            do_git_rm_cached(repo_root, action, args.execute, log)
        elif kind == "create_from_template":
            do_create_from_template(repo_root, action, args.execute, log, errors, global_vars)
        elif kind == "create_file":
            do_create_file(repo_root, action, args.execute, log, global_vars)
        elif kind == "run":
            do_run(repo_root, action, args.execute, log, errors, global_vars)
        else:
            errors.append(f"unknown action type: {kind}")

    print(json.dumps({
        "mode": "EXECUTED" if args.execute else "DRY-RUN (pass --execute to apply)",
        "steps": log,
        "errors": errors,
    }, indent=2))
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
