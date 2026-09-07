# Well-known naming patterns

Names matching anything below are never findings, however generic they read in isolation — they're established idioms, not comprehension hazards. Read the sections matching the ecosystem(s) [`SKILL.md`](../SKILL.md) step 2 detected, plus **Cross-language** always. This file is maintainer-owned; project-specific additions belong in that project's own `NAMING_GLOSSARY.md`, not here.

Every pattern below is implicitly a suffix/prefix match on an otherwise-meaningful identifier (`OrderFactory`, not bare `Factory`) unless marked *(bare)*. A bare, unqualified use of a pattern word — a class literally named `Handler`, a function literally named `Process` — is not covered by that pattern's exemption and should still be classified normally.

## Cross-language

Standard design-pattern vocabulary: `*Factory`, `*Builder`, `*Adapter`, `*Decorator`, `*Proxy`, `*Facade`, `*Observer`, `*Strategy`, `*Visitor`, `*Command`, `*Iterator`, `*Singleton`, `*Composite`, `*Mediator`, `*Memento`, `*Template` (Template Method), `*Chain` (Chain of Responsibility).

Widely-recognized architectural roles: `*Handler`, `*Provider`, `*Repository`, `*Controller`, `*Service`, `*Middleware`, `*Validator`, `*Serializer`, `*Converter`, `*Listener`, `*Client`, `*Server`, `*Gateway`, `*Cache`.

Generic type-parameter conventions (`T`, `TKey`, `TValue`, `TResult`, `TEntity`, `TItem`, single-letter type parameters generally) — these are meant to be generic; that's the point of a type parameter.

## C# / .NET

`I*` interface prefix, `*Attribute`, `*Exception`, `*EventArgs`, `*Comparer`, `*Options`, `*Extensions` (a static extension-method container), `On*`/`*Changed`/`*Completed`/`*ing`/`*ed` event-handler naming, the `Async` suffix (Task-based Asynchronous Pattern).

`*Base` is exempt only when the type is literally the abstract base class implementing the interface named by the rest of the identifier (`FooBase : IFoo`) — that's the base-class-per-interface convention, not vagueness. A `*Base` type with no corresponding `IFoo` is not covered and should be classified normally.

## JavaScript / TypeScript

`use*` (React hook convention), `*Provider`/`*Context` (React context), `*Reducer`, `*Slice` (Redux/Redux Toolkit), `*Props`, `*Ref`, `on*`/`handle*` event-handler naming, `*.d.ts` ambient declaration files.

## Python

Dunder methods (`__init__`, `__repr__`, ...), the `_private`/`__mangled` prefix convention, `*Mixin`, `*Meta` (metaclass), a `Base*` **prefix** for an abstract base class (Python's idiomatic direction is prefix, not suffix — don't expect `*Base` here the way C# uses it), a module-level `main()`.

## Go

The `*er` suffix naming an interface by its single method (`Reader`, `Writer`, `Closer`, and coinages following the same shape) — this is the idiomatic Go interface-naming convention, not vagueness. The `New*` constructor-function convention. Short, package-qualified local names (`buf.Read`, a `db` variable in a database package) are Go's idiom for relying on package qualification for context — don't hold Go's terse local names to another ecosystem's naming bar.

## Java / Kotlin

`*Impl`, `*DTO`, `*DAO`, `*Builder`, `*Factory`, `*Service`, `*Repository`, `*Controller`, `*Exception`, `*Listener`, `On*Listener`.

## Rust

Trait names describing a capability with no `Trait` suffix (`Read`, `Write`, `From`, `Into`, `TryFrom` and coinages following the same shape) — idiomatic Rust omits the suffix a reader might expect from another language. The `*Error` suffix for an error type. `impl` blocks named purely after the type/trait they implement.

## Default-vague seed list

Not an exemption list — the opposite. Absent a domain-specific qualifier, these are default candidates for tier 3 (ambiguous/vague) or tier 4 (generic-but-unambiguous), across every ecosystem: `Manager`, `Helper`, `Util`/`Utils`, `Common`, `Misc`, bare `Data`, bare `Info`, bare `Object`, `Item`, `Thing`, `Stuff`, bare `Base` (outside the C# base-class-per-interface case above), bare `Handler`/`Process`/`Processor`, `temp`/`tmp`, and placeholder names (`foo`/`bar`/`baz`, `test1`, `asdf`). A qualifier that actually narrows the meaning moves a name off this list — `ConnectionManager` names real lifecycle ownership; bare `Manager` doesn't say what's being managed.
