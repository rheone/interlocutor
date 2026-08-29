// netstandard2.0 predates C# 9's init-only setters and records, both of which the compiler recognizes only
// by the presence of this exact marker type. Roslyn analyzer/generator projects must target netstandard2.0
// (see Interlocutor.Generator.csproj's remarks), so this polyfill is required to use `record`/`init` here -
// it is a compile-time-only marker with no runtime behavior, the same shim the C# compiler itself documents.
namespace System.Runtime.CompilerServices;

/// <summary>Polyfill enabling C# 9 <see langword="init"/> accessors and records on netstandard2.0.</summary>
#pragma warning disable S2094 // Intentionally empty: the compiler recognizes this type by its mere presence, not any member.
internal static class IsExternalInit;
#pragma warning restore S2094
