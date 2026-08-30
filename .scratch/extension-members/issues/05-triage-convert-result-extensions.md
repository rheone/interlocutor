# 05 - Triage: convert ResultExtensions to extension members

**What to build:** Evaluate and, if the evaluation holds up, convert `Interlocutor.AspNetCore`'s
`ResultExtensions` - the `ToActionResult`/`ToMinimalApiResult` pair for each of the six `OperationOutcome`
arities, plus the private `OutcomeHttpMapping` rendering pair - to C# 14 extension-member blocks, one
block per receiver type. This is filed as a triage ticket rather than a committed conversion because it's
the highest-risk file in this effort:

- Roughly a dozen XML doc `<see cref>`s cross-reference sibling overloads by their unqualified name
  (e.g. the minimal-API overload's doc pointing at its controller-based counterpart); each needs to become
  a fully class-qualified cref once its target moves inside an extension block, the same fix as ticket 02
  but at much higher volume.
- The file's own `<remarks>` document a deliberate overload-resolution ordering between the six dedicated
  arity overloads and the reflective catch-all overload (a constructed generic parameter beats a bare type
  parameter, and a required `caseRegistry` parameter keeps the catch-all's arity strictly higher than any
  zero/one-argument dedicated-overload call). Regrouping methods under extension blocks must not change
  which overload a given call site resolves to - this needs explicit verification, not just "it compiles."
- Roughly eleven existing `PublicAPI.Unshipped.txt` entries need matching extension-block-shaped entries
  added (additive, per the same mechanics as tickets 02-04, just at this file's scale).

If the evaluation surfaces a reason the conversion shouldn't proceed (an overload-resolution change that
can't be preserved, primarily), the outcome is a documented decision to leave the file on classic syntax,
not a forced conversion.

**Blocked by:** None to *start* the evaluation - can begin anytime. Tickets 02-04 aren't a hard
prerequisite, but landing them first gives this ticket worked examples of the cref-qualification and
`PublicAPI.Unshipped.txt` mechanics at smaller scale.

**Status:** ready-for-agent

- [ ] Every sibling cref in the file is audited; each is fully qualified where the target moved inside an extension block
- [ ] A test (or explicit manual verification recorded in the PR/commit) confirms each of the six dedicated arity overloads, the void overload, and the reflective catch-all still resolve for the same call shapes they did before conversion
- [ ] `PublicAPI.Unshipped.txt` updated to satisfy the analyzer with no RS0016/RS0017 warnings
- [ ] `dotnet build` and `dotnet test tests/Interlocutor.AspNetCore.Tests` are green
- [ ] `dotnet csharpier .` produces no diff on the touched file
- [ ] If conversion is not pursued, the reason is recorded in this file's Comments section instead
