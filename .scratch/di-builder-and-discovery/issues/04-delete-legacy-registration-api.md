> **Status: done** - merged in `96aee52`.

## Labels

ready-for-agent

## What to build

The contract half of the expand–contract begun in ticket 03. With the builder proven, remove every older path so there is exactly one way to configure the library and no way to accidentally get the old implicit behavior.

Delete both container-specific options types, every legacy registration overload including the entry-assembly convenience form, and the flag that silently enabled the default behavior chain. Migrate all in-repo callers and tests to the builder.

The user-visible outcome: a developer cannot accidentally inherit five pipeline behaviors and three service registrations they never asked for, because the code path that did that no longer exists.

This is pre-1.0. Ship no compatibility shims and no obsolete-marked forwarding methods.

## Acceptance criteria

- [ ] Both options types are deleted
- [ ] Every legacy registration overload is deleted, with no forwarding shims
- [ ] The default-behavior-chain flag and its registration path are gone
- [ ] All tests and in-repo callers use the builder
- [ ] The full test suite passes
- [ ] Formatting check passes

## Blocked by

03 - Builder API, alongside the legacy overloads
