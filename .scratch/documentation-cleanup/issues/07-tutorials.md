> **Status: open**

## Labels

ready-for-agent

## What to build

- Review pass over `documentation/sample-app-walkthrough.md`: verify against the actual `samples/` project, remove stale language, add glossary links where appropriate.
- Review pass over `documentation/beginners-guide-todo-api.md`: **stays exactly one file, never split.** Fix stale language and verify accuracy, but be conservative with glossary-linking here - it's a self-contained tutorial, so most terms are already explained in context and shouldn't get an additional glossary link too.
- Move `maintainer_docs/guides/composing-operationoutcome-arities.md` to `documentation/composing-operationoutcome-arities.md` (it's consumer-relevant guidance already cited from `documentation/messages-and-result-types.md` today - a house-style violation this move resolves). Fix the inbound link in `messages-and-result-types.md` and any other references. Add it to `documentation/index.md`'s table of contents. Remove its entry from `maintainer_docs/README.md`'s "Contributor guides" section.

## Acceptance criteria

- [ ] `beginners-guide-todo-api.md` remains a single file
- [ ] `composing-operationoutcome-arities.md` lives at `documentation/composing-operationoutcome-arities.md`, linked from `documentation/index.md`
- [ ] No remaining reference to its old `maintainer_docs/guides/` location
- [ ] No stale language remains in either tutorial file
