---
diagram: GitGraph (Git) Diagram
slug: gitgraph
status: stable
mermaid_version_introduced: "long-standing (pre-v10)"
mermaid_version_verified: "11.16.1"
keyword: gitGraph
source: https://mermaid.js.org/syntax/gitgraph.html
last_verified: 2026-08-09
plugin_required: false
gitlab_compatible: true
---

# GitGraph (Git) Diagram

> **Status:** Stable - introduced long-standing (pre-v10).

## Overview
A GitGraph diagram visualizes git commit history and branch topology - commits, branch creation, checkouts, merges, and cherry-picks - laid out along a timeline. The core mental model is a rendered `git log --graph`: a picture of how branches diverge from and reconverge with each other over a sequence of commits.

## Best-fit uses
- Visualizing a repository's branching, merge, and commit history
- Documenting a proposed or actual git branching strategy for a team
- Illustrating a release/hotfix workflow (e.g. git-flow) with merges
- Showing cherry-pick relationships between commits on different branches

## When NOT to use this
- Generic process flow unrelated to git history - see `flowchart.md` instead.
- Ordered interactions between people or systems in general (this diagram is git-commit-specific) - see `sequence.md` instead.

## Basic syntax
- Start keyword: `gitGraph`, optionally followed by an orientation suffix: `gitGraph LR:` (left-to-right, the default), `gitGraph TB:` (top-to-bottom), `gitGraph BT:` (bottom-to-top).
- `commit [id: "text"] [tag: "text"] [type: NORMAL|REVERSE|HIGHLIGHT]` - records a commit on the current branch; a plain `commit` auto-generates an id.
- `branch name [order: n]` - creates a new branch and switches to it.
- `checkout name` / `switch name` - interchangeable; sets the current active branch.
- `merge name [id: "text"] [tag: "text"] [type: TYPE]` - merges the named branch into the current branch.
- `cherry-pick id: "commit_id" [parent: "parent_id"]` - replays a specific commit onto the current branch.
- The default root branch is `main`, and it starts out checked out.
- Comments: `%% comment text`.

## Simple example
```mermaid
gitGraph
   commit
   commit
   branch develop
   checkout develop
   commit
   checkout main
   merge develop
```
Each bare `commit` auto-generates its id; `branch develop` both creates the branch and switches to it in one step, so the following `checkout develop` is redundant here but shown for clarity.

## Complex example
```mermaid
gitGraph
   commit id: "init"
   commit id: "setup-ci"
   branch develop order: 1
   checkout develop
   commit id: "feat-a"
   commit id: "feat-b"

   branch feature/login order: 2
   checkout feature/login
   commit id: "login-ui"
   commit id: "login-api" tag: "v0.1-alpha"

   checkout develop
   merge feature/login id: "merge-login"

   checkout main
   cherry-pick id: "login-api" parent: "login-ui"
   commit id: "hotfix" type: HIGHLIGHT

   checkout develop
   commit id: "feat-c"
   merge main id: "sync-hotfix"

   checkout main
   merge develop id: "release-1.0" tag: "v1.0.0" type: REVERSE
```
`order:` pins each branch's lane position in the rendered graph regardless of declaration order; the `cherry-pick` onto `main` requires `login-api` to have been given an explicit `id:` earlier and a `parent:` because it followed a merge; `type: HIGHLIGHT` and `type: REVERSE` change a commit's rendered marker shape.

## Escaping & special characters
- Any `id:`, `tag:`, or commit/branch text containing spaces or punctuation must be double-quoted, e.g. `commit id: "release candidate 1"`.
- Keep branch names to alphanumerics, dashes, underscores, and slashes (e.g. `feature/login`) for reliable parsing; quote anything more exotic where the grammar allows it.
- Avoid a literal triple-backtick sequence inside any quoted id/tag text; bump the *outer* fence wrapping this whole mermaid block to four backticks if unavoidable.

## Common pitfalls
- Cherry-picking a commit that was never given an explicit `id:` - `cherry-pick` can only target commits declared with `commit id: "..."`.
- Cherry-picking from the branch you're currently checked out on - the source commit must be reachable from a different branch.
- Issuing `cherry-pick` before the current branch has at least one commit of its own.
- Omitting `parent:` when cherry-picking a merge commit - merge commits require the immediate parent to be specified explicitly.
- Confusing `checkout` (switch to an existing branch) with `branch` (create a new branch and switch to it) - `checkout` on a branch that doesn't exist yet is an error.
- Relying on declaration order alone for lane placement instead of the `order:` attribute when exact lane position matters.

## Beta/experimental caveats
N/A - stable diagram type, no known compatibility caveats.

## Platform compatibility

**GitLab:** Likely compatible - GitLab.com documents supporting Mermaid version 10 (checked 2026-08-15, [source](https://docs.gitlab.com/user/markdown/#diagrams-and-flowcharts)), and this type was introduced in a pre-v10 release, at or before that line. GitLab's pinned version moves independently of this skill - reconfirm against the source before depending on it for anything long-lived.

**GitHub:** Unconfirmed either way - GitHub's own docs don't publish an exhaustive list of supported Mermaid diagram types (they name "flowcharts, sequence diagrams, pie charts and more" as examples, not a complete list), and community reports on which newer types render are inconsistent/contradictory. Don't promise this type renders on GitHub without testing it there directly (e.g. a scratch gist or PR comment).

**VS Code / Obsidian / Notion / similar:** Depends entirely on that specific tool's own bundled or extension-provided Mermaid version, which updates independently of this skill's pin and of every other tool in this list - there is no single answer here. As a stable, long-established diagram type, editor/notes-app Mermaid bundles are more likely (but not guaranteed) to be current enough to support it. Confirm in the specific target tool before relying on this type there.

## Further reading
- https://mermaid.js.org/syntax/gitgraph.html
