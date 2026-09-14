# 09 — Audit and correct Persistence.EntityFrameworkCore's XML docs and code comments

**What to build:** Every public XML doc comment and code comment in `Interlocutor.Persistence.EntityFrameworkCore` accurately describes the shipped API's actual contract and behavior.

**Blocked by:** None — can run independently.

**Status:** done - implemented in `82404e3`

- [x] Every doc comment and code comment in the project is reviewed against the full set of established XML-documentation and code-comment rules and corrected where it falls short.
- [x] Missing XML documentation on public members is added.
- [x] The project builds clean; existing tests are unaffected.
