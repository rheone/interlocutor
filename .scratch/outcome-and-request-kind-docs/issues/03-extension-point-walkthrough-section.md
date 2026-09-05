# 03 - Extension-point doc section: tie SendWelcomeEmail into the walkthrough

Status: ready-for-agent

## Labels

ready-for-agent

## What to build

Add a new subsection to `documentation/sample-app-walkthrough.md` that walks through the `SendWelcomeEmail` feature (issue 02) end-to-end, and cross-references the existing "IOutcome as an Extension Point" mechanism already documented in `documentation/messages-and-result-types.md` (the hand-rolled `IOutcome` implementer plus `OutcomeCaseRegistryBuilder` pattern).

Make explicit, using this concrete instance, that the compiler-generated `Outcome<...>` union shown everywhere else in the tutorial is the library's built-in default - not the only legal `IOutcome` shape a handler can return. Keep the link one-directional: this section points at `messages-and-result-types.md` for the mechanism rather than duplicating it, and `messages-and-result-types.md` itself is not modified by this ticket.

Keep this example's writeup clearly separate from the query/command/unit-command/messaging decision guide (issue 01) - it teaches a different, narrower point (the union isn't mandatory) and shouldn't be folded into that guide's "when to use which" framing.

## Acceptance criteria

- [ ] New subsection walks through `SendWelcomeEmail`'s actual shipped shape (handler, fake SDK client, custom `IOutcome` type, registry registration) - every code reference matches the code added in issue 02, not anticipated ahead of it
- [ ] Cross-references `documentation/messages-and-result-types.md`'s existing extension-point section rather than duplicating its mechanism explanation
- [ ] `documentation/messages-and-result-types.md` itself is unmodified by this ticket
- [ ] Kept as a distinct subsection from the decision guide added in issue 01 - not merged into it
- [ ] No citations to `maintainer_docs/` paths (house style)
- [ ] No enumerated counts in prose beyond the closed-set exception (house style)

## Blocked by

02 - the doc section describes shipped code, not anticipated code.
