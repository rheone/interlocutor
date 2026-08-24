# GitHub CLI for repo setup

Use `gh` to set up the GitHub-side of a new .NET library repo without leaving
the terminal. Run after `git init` + first commit.

## Create the remote repo

```bash
# Create a new public repo under your org/account
gh repo create {{GitHubOrg}}/{{GitHubRepo}} --public --source=. --remote=origin --push

# Create a private repo
gh repo create {{GitHubOrg}}/{{GitHubRepo}} --private --source=. --remote=origin --push

# Create without pushing (manual push later)
gh repo create {{GitHubOrg}}/{{GitHubRepo}} --public --source=.
```

## Repository settings

```bash
# Enable auto-merge (squash)
gh repo edit --enable-auto-merge --delete-branch-on-merge

# Set default branch protections (requires admin)
gh api repos/{{GitHubOrg}}/{{GitHubRepo}}/branches/main/protection \
  --method PUT \
  --input <(echo '{"required_status_checks":null,"enforce_admins":true,"required_pull_request_reviews":null,"restrictions":null}')

# View repo settings
gh repo view --web
```

## Secrets for CI/CD

```bash
# Set a NuGet API key for publish workflow
gh secret set NUGET_API_KEY --body "your-key-here"

# Set for org-level (if the action is at org level)
gh secret set NUGET_API_KEY --body "your-key-here" --org {{GitHubOrg}}

# List existing secrets (shows names only, not values)
gh secret list
```

## Workflows

```bash
# View workflow runs
gh run list

# Watch a run live
gh run watch <run-id>

# View a specific workflow
gh workflow list

# Trigger a workflow manually
gh workflow run build.yml

# Enable a workflow (disabled workflows won't run)
gh workflow enable build.yml
```

## Labels

```bash
# Create standard labels
gh label create "bug" --color d73a4a --description "Something isn't working"
gh label create "enhancement" --color a2eeef --description "New feature or request"
gh label create "documentation" --color 0075ca --description "Docs improvements"
gh label create "dependencies" --color 0366d6 --description "Dependency updates"
```

## Other useful commands

```bash
# Clone an existing repo (handy when setting up a sister repo)
gh repo clone {{GitHubOrg}}/{{GitHubRepo}}

# Create a PR
gh pr create --title "Initial scaffold" --body "Bootstrapped by csharp-library-repo-structure skill."

# View PR checks
gh pr checks
```
