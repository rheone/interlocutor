---
layout: Conceptual
title: Union types - C# reference | Microsoft Learn
canonicalUrl: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/union
apiPlatform: dotnet
author: BillWagner
breadcrumb_path: /dotnet/breadcrumb/toc.json
feedback_system: OpenSource
feedback_product_url: https://aka.ms/feedback/report?space=61
ms.author: wiwagn
ms.devlang: csharp
ms.service: dotnet-csharp
ms.topic: language-reference
show_latex: true
uhfHeaderId: MSDocsHeader-DotNetCSharp
ms.subservice: lang-reference
ms.update-cycle: 1095-days
description: Learn about union types in C#. Unions express values from a closed set of types with exhaustive pattern matching support.
ms.date: 2026-06-05T00:00:00.0000000Z
ai-usage: ai-assisted
locale: en-us
document_id: 0cb4f7b9-7f0a-7e05-327c-e9c3ee03221a
document_version_independent_id: 39218c8d-5d3f-ad57-b214-1b2862296ebf
updated_at: 2026-06-11T20:09:00.0000000Z
original_content_git_url: https://github.com/dotnet/docs/blob/live/docs/csharp/language-reference/builtin-types/union.md
gitcommit: https://github.com/dotnet/docs/blob/f55251e6ed5af81f4500e9c6157e081324ea68ae/docs/csharp/language-reference/builtin-types/union.md
git_commit_id: f55251e6ed5af81f4500e9c6157e081324ea68ae
site_name: Docs
depot_name: VS.core-docs
page_type: conceptual
toc_rel: ../toc.json
pdf_url_template: https://learn.microsoft.com/pdfstore/en-us/VS.core-docs/{branchName}{pdfName}
feedback_help_link_type: ''
feedback_help_link_url: ''
search.mshattr.devlang: csharp
word_count: 2480
asset_id: csharp/language-reference/builtin-types/union
moniker_range_name:
monikers: []
item_type: Content
source_path: docs/csharp/language-reference/builtin-types/union.md
cmProducts:
- https://microsoft-devrel.poolparty.biz/DevRelOfferingOntology/c6f99e62-1cf6-4b71-af9b-649b05f80cce
- https://authoring-docs-microsoft.poolparty.biz/devrel/8b896464-3b7d-4e1f-84b0-9bb45aeb5f64
spProducts:
- https://microsoft-devrel.poolparty.biz/DevRelOfferingOntology/3f56b378-07a9-4fa1-afe8-9889fdc77628
- https://authoring-docs-microsoft.poolparty.biz/devrel/b1d2d671-9549-46e8-918c-24349120dbf5
platformId: 82841458-14a2-7ffb-b31d-a83c2619c5cc
---

# Union types - C# reference | Microsoft Learn

A *union type* represents a value that can be one of several *case types*. Unions provide implicit conversions from each case type, exhaustive pattern matching, and enhanced nullability tracking. Use the `union` keyword to declare a union type:

```csharp
public union Pet(Cat, Dog, Bird);
```

This declaration creates a `Pet` union with three case types: `Cat`, `Dog`, and `Bird`. You can assign any case type value to a `Pet` variable. The compiler ensures that `switch` expressions cover all case types.

The C# language reference documents the most recently released version of the C# language. It also contains initial documentation for features in public previews for the upcoming language release.

The documentation identifies any feature first introduced in the last three versions of the language or in current public previews.

Tip

To find when a feature was first introduced in C#, consult the article on the [C# language version history](../../whats-new/csharp-version-history).

Declare a union when a value must be exactly one of a fixed set of types and you want the compiler to enforce that every possibility is handled. Common scenarios include:

- **Result-or-error returns**: A method returns either a success value or an error value, and the caller must handle both. A union like `union Result(Success, Error)` makes the set of outcomes explicit.
- **Message or command dispatching**: A system processes a closed set of message types. A union ensures new message types produce compile-time warnings at every `switch` that doesn't handle them yet.
- **Replacing marker interfaces or abstract base classes**: If you use an interface or abstract class solely to group types for pattern matching, a union gives you exhaustiveness checking without requiring inheritance or shared members.

A union differs from other type declarations in important ways:

- Unlike a `class` or `struct`, a union doesn't define new data members. Instead, it composes existing types into a closed set of alternatives.
- Unlike an `interface`, a union is closed—you define the complete list of case types in the declaration, and the compiler uses that list for exhaustiveness checks.
- Unlike a `record`, a union doesn't add equality, cloning, or deconstruction behavior. A union focuses on "which case is it?" rather than "what fields does it have?"

## Union declarations

A union declaration specifies a name and a list of case types:

```csharp
public union Pet(Cat, Dog, Bird);
```

*Case types* can be any type that converts to `object`, including classes, structs, interfaces, type parameters, nullable types, and other unions. The following examples show different case type possibilities:

```csharp
public record class Cat(string Name);
public record class Dog(string Name);
public record class Bird(string Name);
```

```csharp
public record class None;
public record class Some<T>(T Value);
public union Option<T>(None, Some<T>);
```

```csharp
public union IntOrString(int, string);
```

When a case type is a value type (like `int`), the value is boxed when stored in the union's `Value` property. Unions store their contents as a single `object?` reference.

A union declaration can include a body with additional members, just like a struct, subject to some restrictions. Union declarations can't include instance fields, auto-properties, or field-like events. You also can't declare public constructors with a single parameter, because the compiler generates those constructors as union creation members. The following `Length` union adds a `TotalMeters` property that uses pattern matching to handle every case type, along with an `Add` method that combines two lengths:

```csharp
public record class Meters(double Value);
public record class Feet(double Value);

public union Length(Meters, Feet)
{
    public double TotalMeters => this switch
    {
        Meters m => m.Value,
        Feet f => f.Value * 0.3048,
        _ => throw new InvalidOperationException("The Length has no value."),
    };

    public Length Add(Length other) => new Meters(TotalMeters + other.TotalMeters);
}
```

## Union conversions

An implicit *union conversion* exists from each case type to the union type:

```csharp
static void BasicConversion()
{
    Pet pet = new Dog("Rex");
    Console.WriteLine(pet.Value); // output: Dog { Name = Rex }

    Pet pet2 = new Cat("Whiskers");
    Console.WriteLine(pet2.Value); // output: Cat { Name = Whiskers }
}
```

Union conversions work by calling the corresponding generated constructor. If a user-defined implicit conversion operator exists for the same type, the user-defined operator takes priority over the union conversion. If more than one case type is equally applicable to the source value, the union conversion is ambiguous, and the compiler reports an error. For details on conversion priority, see the [feature specification](../proposals/unions).

A union conversion to a nullable union struct (`T?`) also works when `T` is a union type:

```csharp
static void NullableUnionExample()
{
    Pet? maybePet = new Dog("Buddy");
    Pet? noPet = null;

    Console.WriteLine(Describe(maybePet)); // output: Dog: Buddy
    Console.WriteLine(Describe(noPet));    // output: no pet

    static string Describe(Pet? pet) => pet switch
    {
        Dog d => d.Name,
        Cat c => c.Name,
        Bird b => b.Name,
        null => "no pet",
    };
}
```

## Union pattern matching

When you pattern match on a union type, patterns generally apply to the union's `Value` property, not the union value itself. This "unwrapping" behavior means the union is transparent to pattern matching:

```csharp
static void PatternMatching()
{
    Pet pet = new Dog("Rex");

    var name = pet switch
    {
        Dog d => d.Name,
        Cat c => c.Name,
        Bird b => b.Name,
    };
    Console.WriteLine(name); // output: Rex
}
```

Three patterns are exceptions to this rule: the discard `_` pattern, the `var` pattern, and the `not` pattern apply to the union value itself, not its `Value` property. Use `var` to capture the union value when `GetPet()` returns a `Pet?` (`Nullable<Pet>`):

```csharp
if (GetPet() is var pet) { /* pet is the Pet? value returned from GetPet */ }
```

In logical patterns, each branch follows the unwrapping rule individually. The left branch of an `and` pattern can change the incoming value that the right branch sees. Because the `not` pattern applies to the incoming union value rather than its `Value`, a leading `not null` doesn't unwrap the value for the branch that follows it:

```csharp
GetPet() switch
{
    // 'var pet' captures the Pet?; 'not null' applies to the Pet? value (not pet.Value)
    var pet and not null => ...,
    // 'not null' doesn't unwrap to Pet, so 'var value' still captures the Pet?
    not null and var value => ...,
}
```

Note

Because patterns apply to `Value`, a pattern like `pet is Pet` typically doesn't match, since `Pet` is tested against the *contents* of the union, not the union itself.

### Null matching

For struct unions, the `null` pattern checks whether `Value` is null:

```csharp
static void NullHandling()
{
    Pet pet = default;
    Console.WriteLine(pet.Value is null); // output: True

    var description = pet switch
    {
        Dog d => d.Name,
        Cat c => c.Name,
        Bird b => b.Name,
        null => "no pet",
    };
    Console.WriteLine(description); // output: no pet
}
```

For class-based unions, `null` succeeds when either the union reference itself is null or its `Value` property is null:

```csharp
Result<string>? result = null;
if (result is null) { /* true — the reference is null */ }

Result<string> empty = new Result<string>((string?)null);
if (empty is null) { /* true — Value is null */ }
```

For nullable union struct types (`Pet?`), `null` succeeds when the nullable wrapper has no value or when the underlying union's `Value` is null.

## Union exhaustiveness

A `switch` expression is exhaustive when it handles all case types of a union. The compiler warns only if a case type isn't handled. You don't need to include a discard pattern (`_`) or `var` pattern to match any type when the expression is definitely assigned:

```csharp
static void PatternMatching()
{
    Pet pet = new Dog("Rex");

    var name = pet switch
    {
        Dog d => d.Name,
        Cat c => c.Name,
        Bird b => b.Name,
    };
    Console.WriteLine(name); // output: Rex
}
```

If the null state of the union's `Value` property is "maybe null," you must also handle `null` to avoid a warning:

```csharp
static void NullHandling()
{
    Pet pet = default;
    Console.WriteLine(pet.Value is null); // output: True

    var description = pet switch
    {
        Dog d => d.Name,
        Cat c => c.Name,
        Bird b => b.Name,
        null => "no pet",
    };
    Console.WriteLine(description); // output: no pet
}
```

This situation can arise when the `union` expression is the default value or isn't definitely assigned, as shown in the preceding sample.

## Nullability

The compiler tracks the null state of a union's `Value` property through the following rules:

- The default null state of a union's `Value` property is "maybe null" if the default null state of any case type is "maybe null." Otherwise, the default null state is "not null."
- When you create a union value from a case type (through a constructor or union conversion), `Value` gets the null state of the incoming value.
- When the non-boxing access pattern's `HasValue` or `TryGetValue(...)` members query the union's contents, the null state of `Value` becomes "not null" on the `true` branch.

## Custom union types

The compiler converts a `union` declaration to a `struct` declaration. The struct is marked with the `[System.Runtime.CompilerServices.Union]` attribute and implements the `IUnion` interface. It includes a public constructor and an implicit conversion for each case type along with a `Value` property. That generated form is opinionated. It's always a struct, always boxes value-type cases, and always stores contents as `object?`.

You might need different behavior if you want to adapt an existing type, create a class-based union, or use a custom storage strategy, or if you need interop support. You can create a union type manually.

Any class or struct with a `[Union]` attribute is a *union type* if it follows the *basic union pattern*. The basic union pattern requires:

- A `[Union]` attribute on the type.
- One or more public constructors, each with a single by-value or `in` parameter. The parameter type of each constructor defines a *case type*.
- A public `Value` property of type `object?` (or `object`) with a `get` accessor.

All the preceding union members must be public. The compiler uses these members to implement union conversions, pattern matching, and exhaustiveness checks. You can also implement the non-boxing access pattern or create a class-based union type. Your custom union type can add additional members.

The compiler assumes that custom union types satisfy these behavioral rules:

- **Soundness**: `Value` always returns `null` or a value of one of the case types - never a value of a different type. For struct unions, `default` produces a `Value` of `null`.
- **Stability**: If you create a union value from a case type, `Value` matches that case type (or is `null` if the input was `null`).
- **Creation equivalence**: If a value is implicitly convertible to two different case types, both creation members produce the same observable behavior.
- **Access pattern consistency**: The `HasValue` and `TryGetValue` members, if present, behave equivalently to checking `Value` directly.

The following example shows a custom union type:

```csharp
[System.Runtime.CompilerServices.Union]
public struct Shape : System.Runtime.CompilerServices.IUnion
{
    private readonly object? _value;

    public Shape(Circle value) { _value = value; }
    public Shape(Rectangle value) { _value = value; }

    public object? Value => _value;
}

public record class Circle(double Radius);
public record class Rectangle(double Width, double Height);
```

```csharp
static void ManualUnionExample()
{
    Shape shape = new Shape(new Circle(5.0));

    var area = shape switch
    {
        Circle c => Math.PI * c.Radius * c.Radius,
        Rectangle r => r.Width * r.Height,
    };
    Console.WriteLine($"{area:F2}"); // output: 78.54
}
```

### Non-boxing access pattern

A custom union type can optionally implement the *non-boxing access pattern* to enable strongly typed access to value-type cases without boxing during pattern matching. This pattern requires:

- A `HasValue` property of type `bool` that returns `true` when `Value` isn't `null`.
- A `TryGetValue` method for each case type that returns `bool` and delivers the value through an `out` parameter. `TryGetValue` returns `true` only when `Value` is a non-null value of that case type. The `out` parameter's type is identity-convertible to the case type, or to the underlying value type when the case type is a nullable value type.

```csharp
[System.Runtime.CompilerServices.Union]
public struct IntOrBool : System.Runtime.CompilerServices.IUnion
{
    private readonly int _intValue;
    private readonly bool _boolValue;
    private readonly byte _tag; // 0 = none, 1 = int, 2 = bool

    public IntOrBool(int? value)
    {
        if (value.HasValue)
        {
            _intValue = value.Value;
            _tag = 1;
        }
    }

    public IntOrBool(bool? value)
    {
        if (value.HasValue)
        {
            _boolValue = value.Value;
            _tag = 2;
        }
    }

    public object? Value => _tag switch
    {
        1 => _intValue,
        2 => _boolValue,
        _ => null
    };

    public bool HasValue => _tag != 0;

    public bool TryGetValue(out int value)
    {
        value = _intValue;
        return _tag == 1;
    }

    public bool TryGetValue(out bool value)
    {
        value = _boolValue;
        return _tag == 2;
    }
}
```

```csharp
static void NonBoxingExample()
{
    IntOrBool val = new IntOrBool((int?)42);

    var description = val switch
    {
        int i => $"int: {i}",
        bool b => $"bool: {b}",
    };
    Console.WriteLine(description); // output: int: 42
}
```

The compiler prefers `TryGetValue` over the `Value` property when implementing pattern matching, which avoids boxing value types.

### Union member providers

A union type can delegate its union members to a nested `IUnionMembers` interface. When this interface is present, the compiler looks for `Create` factory methods instead of constructors:

```csharp
[System.Runtime.CompilerServices.Union]
public record class Outcome<T> : Outcome<T>.IUnionMembers
{
    private readonly object? _value;

    private Outcome(object? value) => _value = value;

    public interface IUnionMembers
    {
        static Outcome<T> Create(T? value) => new(value);
        static Outcome<T> Create(Exception? value) => new(value);
        object? Value { get; }
    }

    object? IUnionMembers.Value => _value;
}
```

Union member providers are useful when the union type needs a private constructor or when the creation logic requires a factory pattern, such as with `record class` union types.

### Class-based union types

A class can also be a union type. This type of union is useful when you need reference semantics or inheritance:

```csharp
[System.Runtime.CompilerServices.Union]
public class Result<T> : System.Runtime.CompilerServices.IUnion
{
    private readonly object? _value;

    public Result(T? value) { _value = value; }
    public Result(Exception? value) { _value = value; }

    public object? Value => _value;
}
```

```csharp
static void ClassUnionExample()
{
    Result<string> ok = new Result<string>("success");
    Result<string> err = new Result<string>(new InvalidOperationException("failed"));

    Console.WriteLine(Describe(ok));  // output: OK: success
    Console.WriteLine(Describe(err)); // output: Error: failed

    static string Describe(Result<string> result) => result switch
    {
        string s => $"OK: {s}",
        Exception e => $"Error: {e.Message}",
        null => "null",
    };
}
```

For class-based unions, the `null` pattern matches both a null reference and a null `Value`.

## Union implementation

Union types rely on the `UnionAttribute` and `IUnion` types in the `System.Runtime.CompilerServices` namespace. The runtime includes these types beginning with .NET 11 Preview 5:

```csharp
namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public sealed class UnionAttribute : Attribute;

public interface IUnion
{
    object? Value { get; }
}
```

Union declarations generated by the compiler implement `IUnion`. You can check for any union value at runtime by using `IUnion`:

```csharp
if (value is IUnion { Value: null }) { /* the union's value is null */ }
```

When you declare a `union` type, the compiler generates a struct that implements `IUnion`. For example, the `Pet` declaration (`public union Pet(Cat, Dog, Bird);`) becomes equivalent to:

```csharp
[Union] public struct Pet : IUnion
{
    public Pet(Cat value) => Value = value;
    public Pet(Dog value) => Value = value;
    public Pet(Bird value) => Value = value;
    public object? Value { get; }
}
```

## C# language specification

For more information, see the [Unions](../proposals/unions) feature specification.
