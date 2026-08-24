# 05 - De-duplicate symbol formatting and attribute resolution in the generator

**What to build:** `ReferencedModuleDiscovery` defines its own private `Format(ITypeSymbol)` with an identical `SymbolDisplayFormat` and body to `SymbolFormatting.Format`, rather than reusing it - despite `SymbolFormatting`'s own doc comment stating it exists so all callers "produce identically shaped strings." Separately, `HandlerTypeAnalyzer.ResolveHandlerOverrideAttribute` and `ResolveOrderingAttribute` both loop `candidate.GetAttributes()`, filter by `SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, X)`, check `ConstructorArguments.Length`, and extract an `INamedTypeSymbol` argument - the same shape, twice, differing only in what's collected.

**Blocked by:** None - can start immediately

**Status:** done

- [x] `ReferencedModuleDiscovery` calls `SymbolFormatting.Format` instead of reimplementing it
- [x] `ResolveHandlerOverrideAttribute` and `ResolveOrderingAttribute` share one parameterized helper for the attribute-extraction shape they have in common
- [x] Existing generator snapshot/golden tests still pass unchanged (output strings must remain byte-identical)

## Comments

Verified already implemented: `ReferencedModuleDiscovery` calls `SymbolFormatting.Format` directly (no private reimplementation remains). `HandlerTypeAnalyzer.ResolveHandlerOverrideAttribute`/`ResolveOrderingAttribute` both delegate the shared attribute-extraction loop to `CollectTypeArgumentAttributes`, differing only in how the collected types are formatted afterward. `dotnet test tests/Interlocutor.Generator.Tests` passes (62/62).
