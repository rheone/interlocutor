# 01 — Document analyzer/formatting rule conflicts and open decisions

**What to build:** A findings document that lays out, for each open configuration question, what's currently configured, what the actual gap or contradiction is, the realistic options, and a blank decision field for a human to fill in. No production code or configuration is changed as part of this ticket — a successor ticket does the fixing, once the decisions in this document are made.

**Blocked by:** None — can start immediately.

**Status:** done - implemented in `61ae224`

- [x] The document covers using-directive ordering/placement: the tension between the requested "alphabetical order, System usings first, separated by a blank line" rule and the currently-configured setting that disables blank-line separation between import groups; the currently-unset (default) state of the StyleCop rules governing System-directives-first and alphabetical ordering; and the already-acknowledged `src/`-vs-`tests/` split on using-directive placement relative to the namespace.
- [x] The document covers missing-XML-comment enforcement: where the missing-doc-comment diagnostics currently apply at warning severity and where they're relaxed, and what tightening that would mean for `src/` vs. `tests/`.
- [x] Each item in the document includes: the current configuration (quoted verbatim), the specific gap or contradiction, the realistic resolution options, and a blank field for the human's decision.
- [x] No `.editorconfig`, `stylecop.json`, `Directory.Build.props`, or source file is modified by this ticket — it produces the findings document only.
- [x] The document's own status marker indicates it awaits a human decision before any successor fixing ticket can be marked ready-for-agent.

## How this was verified

Every diagnostic-severity claim below that isn't a direct quote from a config file was confirmed by
actually triggering the rule: a scratch `.cs` file was added to `src/Interlocutor.CommonKernel/` with
deliberately out-of-order `using` directives, built with `dotnet build -t:Rebuild`, the emitted
warnings inspected, and the scratch file deleted immediately afterward (`git status` at the end of
this ticket shows no residue). No `.editorconfig`, `stylecop.json`, `Directory.Build.props`, or
committed source file was modified. Directory-tree claims (which files place `using` directives before
vs. after the `namespace` line) were confirmed by scripting a scan of every source `.cs` file under
`src/`, `tests/`, `TutorialProject/`, and `smoketests/`, excluding generated build output under `obj/`
and `bin/` (that output's file count shifts with build state — e.g. source-generator output appears
only after a build has run — so it's excluded from the scan rather than quoted as a fixed count).

---

## Finding 1 — Using-directive ordering and placement

### 1a. Blank-line separation between `System` and other usings

**Current configuration** (`.editorconfig`, `#### .NET Coding Conventions ####` section):

```
# Organize usings
dotnet_separate_import_directive_groups = false
dotnet_sort_system_directives_first = true
file_header_template = unset
```

**The gap:** The requested rule is "alphabetical order, System usings first, **separated by a blank
line**." `dotnet_separate_import_directive_groups` is the Roslyn formatter option that inserts a
blank line between using-directive groups (e.g., between the `System.*` group and everything else).
It is explicitly set to `false` here — the opposite of what the requested rule asks for. This isn't an
oversight buried in an unrelated default; it's a direct, named setting that contradicts the requested
rule's third clause.

**Realistic options:**

- **Option A:** Flip `dotnet_separate_import_directive_groups` to `true`. This is the direct fix for
  the blank-line clause, but it changes the formatted shape of every multi-group using block in the
  repo (anywhere `System.*` usings sit next to non-`System.*` ones) the next time `dotnet csharpier .`
  or an IDE-driven cleanup runs.
- **Option B:** Keep `false` and drop the blank-line clause from the requested rule, treating "System
  first, alphabetical" as the enforced part and blank-line separation as a non-goal. csharpier does
  not manage using-directive blank lines either way, so nothing about this option requires a
  csharpier config change.
- **Option C:** Keep `false` for now and revisit only if/when a broader formatting pass touches using
  blocks anyway (bundling the churn with other formatting-alignment work rather than a standalone
  reformat).

**Decision:** Option A. Flip `dotnet_separate_import_directive_groups` to `true`; the successor ticket
runs `dotnet csharpier .` afterward to absorb the resulting reformat churn across the repo.

### 1b. System-directives-first and alphabetical ordering (StyleCop SA1208 / SA1210)

**Current configuration:** Neither `dotnet_diagnostic.SA1208.severity` nor
`dotnet_diagnostic.SA1210.severity` appears anywhere in `.editorconfig` (confirmed by search — no
match for either diagnostic ID in the file). Both are therefore governed entirely by StyleCop
Analyzers' own shipped default severity, not by any explicit repo setting.

**Verified actual behavior:** A scratch file was added to `src/Interlocutor.CommonKernel/` with usings
in the order `System.Text`, `Interlocutor.CommonKernel`, `System.Collections.Generic`, `System` (i.e.
a non-`System` namespace interleaved before two `System.*` ones, and the whole block not alphabetized).
Rebuilding produced:

```
warning SA1208: Using directive for 'System.Collections.Generic' should appear before directive for 'Interlocutor.CommonKernel'
warning SA1208: Using directive for 'System' should appear before directive for 'Interlocutor.CommonKernel'
warning SA1210: Using directives should be ordered alphabetically by the namespaces
```

So both rules are live today, at their shipped default of `Warning`, purely because nothing in this
repo's config turns them off or down. This matches "System usings first, alphabetical order" — the
first two clauses of the requested rule are **already enforced** and already producing build warnings
on violation. There's no code/config change needed for those two clauses; the gap is entirely in 1a
(the blank-line clause) and in the fact that this enforcement is currently implicit (relies on
"nobody has set it to `None`") rather than a recorded, intentional setting.

This isn't only a synthetic finding: a full solution build (`dotnet build`, run as part of this
ticket's own validation step) already surfaces one live, pre-existing SA1210 violation unrelated to
this ticket's changes — `tests/Interlocutor.Persistence.NHibernate.Tests/RealSessionFixture.cs` orders
its usings `System.Data.SQLite`, `global::NHibernate`, `FluentNHibernate.Cfg`,
`FluentNHibernate.Cfg.Db`, `FluentNHibernate.Mapping`, `global::NHibernate.Tool.hbm2ddl` — not
alphabetical — and the build already warns on it today, independent of anything this ticket touched.

**Realistic options:**

- **Option A:** Leave both rules on their implicit default and add an explicit
  `dotnet_diagnostic.SA1208.severity = Warning` / `dotnet_diagnostic.SA1210.severity = Warning` pair to
  `.editorconfig` purely for self-documentation — matching the pattern the rest of the SA1600 family
  below already follows (explicit severities with a rationale comment) rather than relying on an
  un-stated shipped default that a future StyleCop upgrade could silently change.
- **Option B:** Leave it entirely implicit, on the grounds that an unset diagnostic already falling
  back to a sensible default doesn't need a line of config to prove it.
- **Option C:** Raise either or both to `Error` if the human wants using-order violations to fail the
  build rather than merely warn (this repo currently keeps `TreatWarningsAsErrors` commented out in
  `Directory.Build.props`, so today nothing here fails a build either way).

**Decision:** Option A. Add explicit `dotnet_diagnostic.SA1208.severity = Warning` and
`dotnet_diagnostic.SA1210.severity = Warning` to `.editorconfig`, matching the SA1600-family pattern of
recorded, self-documenting severities rather than relying on StyleCop's unstated shipped default. Not
raised to `Error`.

### 1c. `src/`-vs-`tests/` split on using-directive placement relative to the namespace

**Current configuration** (`stylecop.json`):

```json
"orderingRules": {
  "usingDirectivesPlacement": "insideNamespace"
}
```

(`csharp_using_directive_placement = inside_namespace:silent` in `.editorconfig` matches this.)

**Current configuration** (`.editorconfig`, immediately above the SA1200 line):

```
# Warning, not error, until tests/ is migrated: src/ puts usings inside the namespace (matching
# stylecop.json insideNamespace), tests/ puts them outside. Restore to error once tests/ is unified.

dotnet_diagnostic.SA1200.severity = Warning      # Using directives should be inside namespace
```

**The gap/contradiction:** This comment (introduced in the very first scaffolding commit,
`f04fbde0`) says `tests/` currently places `using` directives *outside* the namespace (i.e., above a
block-scoped `namespace` line, or before a file-scoped one), and that SA1200 is deliberately held at
`Warning` instead of `Error` until that's fixed. A repo-wide scan of every source `.cs` file's actual `using` vs.
`namespace` line order — across `src/`, `tests/`, `TutorialProject/`, and `smoketests/`, excluding
generated build output under `obj/`/`bin/` — found **zero** files anywhere in the tree that place a
`using` directive before the `namespace` line. Every file already puts usings inside/after the namespace
declaration, matching `stylecop.json`'s `insideNamespace` setting. `git log --all` for the area shows
no dedicated "migrate tests using placement" commit either — the comment's premise appears to already
be stale, and the underlying split it describes does not exist in the current tree. It's possible new
test files were simply always written to match `src/`'s convention (by copy-paste habit or IDE
default) rather than through a deliberate migration, and the `Warning`-not-`Error` severity plus its
rationale comment were never revisited afterward. This SA1200 severity also isn't scoped per-directory
— it's set once in the solution-wide `[*.{cs,vb,fs}]` section, so today it's uniformly `Warning`
everywhere, not `Warning` in `tests/` and `Error` in `src/`.

**Realistic options:**

- **Option A:** Since the split the comment describes no longer exists, restore
  `dotnet_diagnostic.SA1200.severity` to `Error` (or at least drop the now-stale
  "until tests/ is migrated" comment and re-justify whatever severity is chosen) and delete the dead
  rationale.
- **Option B:** Re-run the same file-by-file scan as part of the fixing ticket itself (not just trust
  this document) immediately before changing the severity, in case a file was added between this
  ticket and the fixing ticket that reintroduces the outside-namespace pattern.
- **Option C:** Leave SA1200 at `Warning` regardless of whether the split still exists, if the human
  decides `Warning` is the right steady-state severity for this rule independent of the original
  tests/-migration rationale.

**Decision:** Option A, with Option B's re-verification folded in as process rather than a separate
decision. Restore `dotnet_diagnostic.SA1200.severity` to `Error` and delete the stale "until tests/ is
migrated" comment. The successor ticket re-runs the same repo-wide using-vs-namespace-placement scan as
its first step, immediately before flipping the severity, in case a file changed between this document
and that ticket landing.

---

## Finding 2 — Missing-XML-comment enforcement (`src/` vs. `tests/`)

**Current configuration — solution-wide baseline** (`.editorconfig`, under the un-headered
`[*.{cs,vb,fs}]` section that runs from line 34 to line 673, i.e. applies to every `.cs` file in the
solution — `src/`, `tests/`, `TutorialProject/`, `smoketests/` alike — unless a later, more specific
section overrides it):

```
dotnet_diagnostic.SA1600.severity = Warning	     # A C# code element is missing a documentation header.
dotnet_diagnostic.SA1601.severity = Warning	     # A C# partial element is missing a documentation header.
dotnet_diagnostic.SA1602.severity = Warning	     # An item within a C# enumeration is missing an Xml documentation header.
```

(and the rest of the SA1603–SA1651 documentation-content family, also `Warning`, covering malformed/
missing `<param>`, `<returns>`, `<typeparam>`, `<summary>` content, etc.)

**Current configuration — CS1591**, the compiler's own missing-doc diagnostic, enabled repo-wide
because `Directory.Build.props` sets `<GenerateDocumentationFile>true</GenerateDocumentationFile>` for
every project (including test projects — this property isn't scoped to packable projects only). No
`.editorconfig` entry overrides `dotnet_diagnostic.CS1591.severity` outside the one narrow section
below, so it also runs at its shipped default (`Warning`) everywhere else.

**Current configuration — the test-file relaxation** (`.editorconfig`,
`# C# Tests files in Tests projects` section):

```
[{**Tests/**Tests.cs,**Tests.**/**Tests.cs}]
# Reset StyleCop documentation comments
dotnet_diagnostic.SA1600.severity = Suggestion	# A C# code element is missing a documentation header.
dotnet_diagnostic.SA1601.severity = Suggestion	# A C# partial element is missing a documentation header.
dotnet_diagnostic.SA1602.severity = Suggestion	# An item within a C# enumeration is missing an Xml documentation header.
...
dotnet_diagnostic.CS1591.severity = Suggestion # Missing XML comment for publicly visible type or member 'Type_or_Member'
```

**The gap:** This relaxation glob (`{**Tests/**Tests.cs,**Tests.**/**Tests.cs}`) matches only files
whose *name* ends in `Tests.cs` inside a directory whose name contains `Tests` — i.e. the actual
`XxxTests.cs` test-class files. It is narrower than the `[tests/**/*.cs]` glob used elsewhere in the
same file for the NSubstitute/xUnit/VSTHRD200/IDisposableAnalyzers overrides, which covers the whole
`tests/` tree — "fixtures, builders, and other test-support files," per that section's own comment.
Concretely: a fixture, builder, test-base-class, or handler-stub file under `tests/` that is **not**
itself named `*Tests.cs` (e.g. `tests/Interlocutor.Abstractions.Tests/Fixtures/SomeFixture.cs`) is
still governed by the solution-wide `Warning` severity for SA1600/SA1601/SA1602 and for CS1591 — full
missing-XML-comment enforcement, the same as `src/`. Building the existing test suite today produces
zero SA16xx warnings, so this isn't currently biting (support files in the tree already carry
documentation, consistent with CLAUDE.md's blanket "rich XML doc comments on all public members"
rule) — but it means the *intent* behind the relaxation ("tests don't need doc comments") and the
*actual scope* of the relaxation ("only files literally named `*Tests.cs`") disagree, and a newly
added, undocumented fixture file would be flagged where a newly added, undocumented `*Tests.cs` file
would not.

A second, narrower gap: only three of the SA16xx family (SA1600, SA1601, SA1602) are reset for
`*Tests.cs` files. The rest of the missing/malformed-doc-content family — SA1611 (missing `<param>`),
SA1615 (missing `<returns>`), SA1618/SA1619/SA1620/SA1621/SA1622 (missing/mismatched `<typeparam>`),
etc. — stays at `Warning` even inside `*Tests.cs` files. In practice this rarely fires because SA1600
already requires *some* doc header before SA1611 etc. would have anything to check against, but the
two settings aren't logically tied together by the config itself.

**What tightening would mean for `src/` vs. `tests/`:**

- `src/` (and `TutorialProject/`, which CLAUDE.md holds to the same documentation bar) is already at
  full `Warning` enforcement for the entire SA16xx family plus CS1591 — there is nothing to "tighten"
  there; it's already the strict end of the spectrum.
- `tests/` is a blend today: `*Tests.cs` files get the relaxed (`Suggestion`) treatment; every other
  file under `tests/` is already held to the `src/`-level `Warning` bar, whether or not that was
  deliberate.

**Realistic options:**

- **Option A:** Widen the relaxation glob from `{**Tests/**Tests.cs,**Tests.**/**Tests.cs}` to the
  same `tests/**/*.cs` shape already used for the NSubstitute/xUnit/VSTHRD200 overrides, so the
  relaxation intentionally covers fixtures/builders/support files too, not just `*Tests.cs` files.
  This is a config-only change (for the successor ticket) but is a real behavior change: it would
  newly permit undocumented fixtures that today would warn.
- **Option B:** Leave the glob exactly as-is, on the theory that test *support* code (fixtures,
  builders, base classes) is reused across many tests and genuinely benefits from the same
  documentation discipline as `src/`, while only the `[Fact]`/`[Theory]` test-method files themselves
  (named for behavior, not for reuse) should be exempt. This matches the existing, if implicit,
  distinction and requires no change at all.
- **Option C:** Go the other direction and tighten `*Tests.cs` files back toward `src/`-level
  enforcement (raise SA1600/SA1601/SA1602/CS1591 for `*Tests.cs` back to `Warning`), if the human
  decides test classes should also carry doc comments on their public members (test classes are
  typically `public` for the xUnit runner to discover them) and the relaxation was never actually
  desired long-term.

**Decision:** Option B — **no configuration change**. The glob stays exactly as it is today, matching
only `*Tests.cs` files; fixtures/builders/support files elsewhere in `tests/` remain at the `src/`-level
`Warning` bar they already carry.

The intent behind this decision was explored more deeply than the three options above capture, and is
worth recording so it isn't re-litigated:

The initial ask was narrower and more precise than "leave as-is" — encourage documentation on
`[Fact]`/`[Theory]`-attributed test methods specifically, without requiring it, while still *requiring*
documentation on every other public member in `tests/` (fixtures, builders, constructors,
`[MemberData]` sources, non-test public members inside `*Tests.cs` classes, etc.), regardless of which
file they live in. That precision isn't achievable with this repo's current tooling: a
`.editorconfig` severity override only operates at file-glob granularity, and StyleCop.Analyzers
(pinned at `1.2.0-beta.556`) has no attribute-aware exemption for SA1600/SA1601/SA1602/CS1591 the way
VSTHRD200 has a built-in exemption for `[Fact]`/`[Theory]` methods — there's no existing
`DiagnosticSuppressor` or per-member `SuppressMessage` precedent anywhere in this codebase to build on
either.

A Roslyn `DiagnosticSuppressor` was considered as a way to get member-level precision (suppress only on
`[Fact]`/`[Theory]` methods, leave every other public member at `Warning`), but a suppressor can only
fully hide an already-reported diagnostic — it cannot downgrade severity to `Suggestion`. That would
deliver "not required" but drop "encouraged" entirely (most IDEs show no squiggle/light-bulb at all for
a suppressed diagnostic by default), which doesn't match the actual ask. A combined
suppressor-plus-companion-analyzer approach (suppress SA1600/CS1591 on test methods, and have a new
small analyzer separately emit its own `Suggestion`-severity "consider documenting this test method"
diagnostic) would deliver both precision and visible encouragement, but was rejected as more
infrastructure than the ask warrants — a new repo-tooling analyzer project, not a config change.

Landing back on **no change** trades member-level precision for two things the alternatives couldn't
all deliver together with existing tooling: a genuinely visible "encouraged, not required" experience
(`Suggestion` severity, not silence), and zero new infrastructure. The imprecision this accepts — a
non-test public member that happens to live in a `*Tests.cs` file gets the same relaxed treatment as
the test methods around it — was judged acceptable, since such members are reused code and the original
Option B rationale (fixtures/builders benefit from the same documentation discipline as `src/`) already
covers the common case of reused test support code, which mostly lives outside `*Tests.cs` files
entirely and was never touched by this relaxation to begin with.

If precise member-level "encouraged, not required" for test methods becomes important enough to justify
new infrastructure later, the suppressor-plus-companion-analyzer combination above is the documented
fallback design — file a fresh ticket against it rather than re-deriving this analysis.

---

## Status of this document

**All four decisions above are now made.** Findings 1a, 1b, and 1c resolve to a config-only successor
ticket; Finding 2 resolves to no change at all (see that section's decision for why, and the documented
fallback design if member-level precision is wanted later). See
`.scratch/analyzer-formatting-alignment/issues/02-apply-using-directive-and-sa1200-config-decisions.md`
for the successor ticket covering 1a/1b/1c.
