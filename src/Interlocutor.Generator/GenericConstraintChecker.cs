namespace Interlocutor.Generator;

/// <summary>
/// Decides whether a concrete type argument satisfies one of an open generic handler's own type parameter
/// constraints (ADR-0008, ticket 11) - the check that keeps a request whose type argument fails a handler's
/// <c>where</c> clause from being closed over that handler.
/// </summary>
/// <remarks>
/// Resolved purely from a pre-built <see cref="GenericConstraintSnapshot"/> (ticket 53) rather than the live
/// <see cref="Microsoft.CodeAnalysis.Compilation"/> - both sides are looked up here by the metadata names each
/// side already captured, but the actual semantic-model resolution happened once, earlier, in
/// <see cref="GenericConstraintSnapshotBuilder.Build"/>, so this method (and everything that calls it) never
/// needs the raw <see cref="Microsoft.CodeAnalysis.Compilation"/> object itself - keeping
/// <see cref="OpenGenericHandlerResolver.Resolve"/>'s own inputs incremental-pipeline-cacheable.
/// </remarks>
internal static class GenericConstraintChecker
{
    /// <summary>
    /// Whether <paramref name="argumentMetadataName"/> satisfies every clause of <paramref name="constraint"/>.
    /// </summary>
    /// <param name="constraint">The open generic handler's type parameter constraint to check against.</param>
    /// <param name="argumentMetadataName">
    /// The concrete type argument's CLR metadata name, or <see langword="null"/> if this generator could not
    /// resolve one for it (a constructed generic, array, or similar shape) - treated as satisfying every
    /// constraint, since it cannot be soundly checked from a metadata name alone.
    /// </param>
    /// <param name="snapshot">The pre-resolved facts to check both sides against - see <see cref="GenericConstraintSnapshot"/>.</param>
    /// <returns><see langword="true"/> if every constraint clause is satisfied (or unresolvable and therefore assumed satisfied).</returns>
    public static bool Satisfies(
        GenericParameterConstraintModel constraint,
        string? argumentMetadataName,
        GenericConstraintSnapshot snapshot
    )
    {
        if (argumentMetadataName is null)
        {
            return true;
        }

        ResolvedArgumentTypeFact? argumentFact = null;
        foreach (var fact in snapshot.ArgumentTypeFacts)
        {
            if (fact.MetadataName == argumentMetadataName)
            {
                argumentFact = fact;
                break;
            }
        }

        if (argumentFact is null)
        {
            // Mirrors the original "GetTypeByMetadataName returned null - cannot soundly check, assume
            // satisfied" fallback: no fact means the snapshot builder could not resolve this type either.
            return true;
        }

        if (constraint.HasValueTypeConstraint && !argumentFact.Value.IsValueType)
        {
            return false;
        }

        if (constraint.HasReferenceTypeConstraint && !argumentFact.Value.IsReferenceType)
        {
            return false;
        }

        if (constraint.HasConstructorConstraint && !argumentFact.Value.HasAccessibleParameterlessConstructor)
        {
            return false;
        }

        // constraint.HasNotNullConstraint is deliberately not checked here: notnull is a nullable-annotation
        // constraint, not a type-identity one. A nullable value type argument (int?) never reaches this method
        // at all - it is a constructed generic (Nullable<T>) and SymbolFormatting.TryGetMetadataName already
        // returns null for every constructed generic, so argumentMetadataName is null and Satisfies returns
        // true above. A nullable-annotated reference type argument resolves to the same metadata name as its
        // non-nullable counterpart (nullability annotation is not part of a type's metadata identity), so
        // there is no way to distinguish the two from argumentMetadataName alone. Both cases fall back to this
        // method's existing "unresolvable from a metadata name -> treated as satisfied" trade-off, the same one
        // applied to every constraint type name the snapshot builder could not resolve.
        foreach (var constraintTypeName in constraint.ConstraintTypeMetadataNames)
        {
            if (constraintTypeName is null)
            {
                // Unresolvable constraint type (itself generic, most commonly) - cannot be soundly checked from
                // a metadata name, so it is treated as satisfied rather than rejected.
                continue;
            }

            var isSatisfying = true;
            var found = false;
            foreach (var fact in snapshot.ConversionFacts)
            {
                if (fact.ArgumentMetadataName == argumentMetadataName && fact.ConstraintTypeMetadataName == constraintTypeName)
                {
                    found = true;
                    isSatisfying = fact.IsSatisfyingConversion;
                    break;
                }
            }

            if (!found)
            {
                // The snapshot builder could not resolve the constraint type either - treated as satisfied,
                // same as the original "constraintType is null -> continue" fallback.
                continue;
            }

            if (!isSatisfying)
            {
                return false;
            }
        }

        return true;
    }
}
