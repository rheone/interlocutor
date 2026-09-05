# Triage Labels

The skills speak in terms of five canonical triage roles. This file maps those roles to the actual label strings used in this repo's issue tracker.

| Label in mattpocock/skills | Label in our tracker | Meaning                                  |
| --------------------------- | --------------------- | ----------------------------------------- |
| `needs-triage`               | `needs-triage`         | Maintainer needs to evaluate this issue   |
| `needs-info`                 | `needs-info`           | Waiting on reporter for more information  |
| `ready-for-agent`            | `ready-for-agent`      | Fully specified, ready for an AFK agent   |
| `ready-for-human`            | `ready-for-human`      | Requires human implementation             |
| `wontfix`                    | `wontfix`              | Will not be actioned                      |

When a skill mentions a role (e.g. "apply the AFK-ready triage label"), use the corresponding label string from this table.

Edit the right-hand column to match whatever vocabulary you actually use.

## The accepted set of `Status:` values

The five triage roles above are only the *pre-work* half of the vocabulary - the state a ticket sits in before anyone has started it. A ticket, PRD, or ADR also needs a way to record that it's finished, abandoned, or decided, and this repo's tracker has settled on a consistent (if informally documented) set of words for that half too. Together, these are the **entire accepted set** - nothing else is a valid `Status:` value:

**Pre-work (the five triage roles):** `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`.

**Terminal, for a PRD or issue file:**
- `done` - implemented; may be followed by free text naming the commit(s) (e.g. `done - merged in `a5d96ca`.`)
- `closed` - used interchangeably with `done` in older tickets; may carry a parenthetical (e.g. `closed (implemented)`)
- `resolved` - the wayfinder map/child-ticket convention's completion word (see `issue-tracker.md`'s Wayfinding section)
- `complete` - a PRD-level synonym for `done` when summarizing a whole feature directory
- `not started` - a PRD whose child issues haven't been picked up yet (e.g. `**not started**. Tracked as issues under ...`)
- `wontfix` followed by a reason (e.g. `wontfix (already implemented)`, `wontfix (superseded - ...)`) - the triage role doubling as a terminal state once the "won't fix" decision is actually recorded
- `superseded` followed by a pointer to what replaced it (e.g. `superseded** - this issue predates the `documentation/` restructure.`)
- `split` followed by a description of how the ticket divided (e.g. `split - generic mapper recognition done; concrete second implementation remains needs-triage`)
- A PRD may roll several of the above up into one narrative sentence (e.g. `units 01-11 and 13-16 done; issues 18-19 done in ce515d2. Only unit 12 ... held `needs-triage``) - this is fine, since the words inside the sentence are themselves each drawn from this set.

**For an ADR** (`maintainer_docs/adr/*.md`), a different, smaller vocabulary applies - the standard ADR lifecycle: `Proposed`, `Accepted` (optionally `Accepted (amended)` or `Accepted (amended - <what>)`), `Rejected`, or `Superseded by [ADR-NNNN](NNNN-slug.md)`. Every ADR in this repo today is `Accepted` or `Superseded by`.

**Not part of the set:** a bare `open`, or any other word not listed above. If you encounter one, it's a tracker defect, not a new state to imitate - see "Rejecting an unrecognized status" below rather than writing more of them.

## Rejecting an unrecognized status

Before writing or changing a `Status:` line on any ADR, PRD, or ticket - whether the new value came from the user, from your own judgment about what state a ticket should move to, or from copying a nearby file's shape - check it against the accepted set above (or the ADR lifecycle words, for an ADR).

If the value isn't in that set:

1. **Do not write it.** Don't create a new status word, and don't silently normalize it to something you guess is close enough.
2. **Stop and tell the user.** Name the file, the rejected value, and the accepted alternative you'd use instead (or the shortlist, if more than one plausibly fits).
3. **Wait for explicit acknowledgment** before writing anything - a plain "yes, use `ready-for-agent`" (or equivalent) counts; proceeding on your own inference of what the user "probably meant" does not.

This applies symmetrically to reading and writing: if you're auditing existing files and find one already carrying an unrecognized value, don't fix it silently either - report it and the correction you'd make, and wait for the same acknowledgment before editing.

When you cannot determine which accepted state actually fits a ticket (as opposed to the value simply being unrecognized), default to `needs-triage` - it exists precisely for "a maintainer needs to evaluate this," which is true whenever an agent isn't confident enough to pick a more specific state.
