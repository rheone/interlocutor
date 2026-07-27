See and understand contents of `bootstrapreference/` (do not modify these files) these are temporary files

The goal is to create an as of yet unnamed mediator implementation using the new .NET 11 Union Types.
What exists is not the definitive answer, but a starting point to concepts and open to suggestions and advancements.

Use multiple agents; there is a lot of information available that will need to be distilled into PRD and ARDs

/grilling
/to-sepc

---

Create a C# solution (slnx) for a library package

Create appropriate projects and testing projects

Use
  - Target .NET 11 preview
  - Directory.Packages.props
  - Directory.Build.props
  - Directory.Build.targets
  - implicit usings / global usings
  - nullability model
  - appropriate analyzer
  - centralized package management
  - TDD
    - Unit Tests (xUnit v3 + NSubsitute)
    - NetArchTest architecture tests project
  - Roslynator.Analyzers + codefixes + formatting + refactoring (all appropriate)
  - Stylecop
  - add dictionary.dic to editor config
  - apply root files appropriately

suggest missing configurations /files

Create a new PRD + ARDs based on the conversation we have
Avoid external dependencies (unless explicitly requested / approved)

/grilling
/to-sepc
