# 07 — Audit and correct FluentValidation's XML docs and code comments

**What to build:** Every public XML doc comment and code comment in `Interlocutor.FluentValidation` accurately describes the shipped API's actual contract and behavior, with no claim that any generic parameter is inherently "the success" case.

**Blocked by:** None — can run independently.

**Status:** done - implemented in `b183c26`

- [x] No doc comment or code comment on `ValidationBehavior` (or elsewhere in the project) names a generic parameter as "the success-type parameter" — parameters are described by what the developer may put there, not by an assumed role.
- [x] Every doc comment and code comment in the project is reviewed against the full set of established XML-documentation and code-comment rules and corrected where it falls short.
- [x] Missing XML documentation on public members is added.
- [x] The project builds clean; existing tests are unaffected.
