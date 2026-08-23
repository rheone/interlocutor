# 05 - CI gate that holds the promise

Type: task
Status: open
Blocked by: 03

## Question

Stand up the continuous integration that makes the committed support level un-regressable.

Ticket 17 of `di-builder-and-discovery` put it plainly: without a gate, an ahead-of-time claim becomes a
false claim within a few commits. This effort has already demonstrated the sharper version of that problem -
a **clean analyzer build is not a passing gate**, because the library built warning-free while throwing on
every dispatch.

So the gate has to include whatever level of execution the promise implies:

- Analyzer-clean build across the shipping libraries - necessary, insufficient on its own.
- Whatever publish mode the promise names, actually performed.
- And, if the promise covers runtime behavior, the sample or an equivalent harness **run** and exercised,
  not merely compiled.

Constraints to design around: this repo has **no CI workflows at all** today (only
`.github/dependabot.yml`), so this is greenfield. The dev container has gcc but no clang, so anything
requiring the native ILCompiler toolchain must be shown to work on the chosen runner image rather than
assumed. The container-compatibility promise from ticket 03 may mean the gate has to run more than once,
once per container.

Take the CI recommendation from **Native AOT feasibility** as input rather than re-researching it.

## Answer

_Pending._
