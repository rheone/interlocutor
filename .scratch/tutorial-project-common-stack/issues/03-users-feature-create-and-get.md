# 03 - Users feature: Create & Get

**What to build:** `User` entity (Id, DisplayName, Email) mapped via EF Core. Create and Get endpoints/commands/queries/handlers, following the vertical-slice convention. `CreateUserCommand` gets a duplicate-email `Conflict` check.

**Blocked by:** 02

**Status:** done

- [x] `POST` creates a user; a duplicate email returns the `Conflict` case, not a generic `Failure`
- [x] `GET` retrieves a user by id, including the not-found case
- [x] Unit tests (mocked repository) and persistence tests (real in-memory SQLite via EF Core) both cover the create/get paths

## Comments

**2026-09-04** - Implemented: `User` entity (`Id: int`, `DisplayName`, `Email`) mapped via EF Core with a unique index on `Email` as schema-level defense-in-depth. `CreateUserCommand: ICommand<Outcome<UserDto, ValidationErrors, Conflict>>` detects a duplicate email via a pre-check query (not a caught `DbUpdateException`) inside its eager `ITransactionalRequest` transaction; `GetUserByIdQuery: IQuery<Outcome<UserDto, NotFound>>`. Both endpoints mapped in `Program.cs`. Unit tests (mocked `IDomainRepository`), persistence tests (real in-memory SQLite), and HTTP tests (through `TodoTasksApiFactory`) all cover create/get including the Conflict/NotFound paths. Along the way, found and fixed a real gap: `ScanAssembly` only discovers handlers, not `IValidator<T>` implementations under MS DI (unlike the shelved Autofac project's `AsClosedTypesOf`) - added `FluentValidation.DependencyInjectionExtensions` and `AddValidatorsFromAssembly` to `Program.cs`. Full solution build (0 errors) and test (1076/1076) green.
