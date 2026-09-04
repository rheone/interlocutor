namespace Interlocutor.Generator.Analysis;

using System;
using System.Linq;
using Interlocutor.Generator.Diagnostics;
using Interlocutor.Generator.Emission;
using Interlocutor.Generator.Models;
using Microsoft.CodeAnalysis;

/// <summary>
/// Reports <see cref="InterlocutorDiagnostics.AccessorNameCollision"/> for one attributed union's
/// <see cref="OutcomeUnionModel.CaseAccessorNames"/>: two peer case types that would generate the same named
/// <c>TryGet{TypeName}</c> accessor, or a case type whose own name collides with a reserved generated member name.
/// </summary>
/// <remarks>
/// Called from <see cref="OutcomeMemberGenerator.Execute"/> once per attributed union, after the arity check
/// (<see cref="InterlocutorDiagnostics.TooFewOutcomeCaseTypes"/>) has already ruled out fewer than two case
/// types - mirroring how <see cref="HandlerDiagnosticAnalyzer"/> is a plain reporting helper driven by an
/// already-built model, not a standalone <see cref="Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer"/>.
/// </remarks>
internal static class OutcomeAccessorCollisionAnalyzer
{
    /// <summary>Reports one diagnostic per colliding case in <paramref name="model"/>.</summary>
    /// <param name="model">The attributed union to check.</param>
    /// <param name="context">The source production context to report diagnostics to.</param>
    /// <returns>
    /// <see langword="true"/> if at least one collision was reported - the caller should skip emitting the
    /// generated partial declaration in that case, the same way it already does for
    /// <see cref="InterlocutorDiagnostics.TooFewOutcomeCaseTypes"/>.
    /// </returns>
    public static bool ReportAccessorNameCollisions(OutcomeUnionModel model, SourceProductionContext context)
    {
        var foundCollision = false;

        var positionsByAccessorName = model
            .CaseAccessorNames.Select(static (name, position) => (name, position))
            .Where(static entry => entry.name is not null)
            .GroupBy(static entry => entry.name, static entry => entry.position, StringComparer.Ordinal);

        foreach (var group in positionsByAccessorName)
        {
            var positions = group.ToList();
            if (positions.Count > 1)
            {
                var collidingCases = string.Join(
                    ", ",
                    positions.Select(position => $"'{model.CaseTypeDisplayStrings[position]}' (position {position + 1})")
                );
                Report(model, context, group.Key!, $"case types {collidingCases} would all generate this accessor");
                foundCollision = true;
            }
        }

        for (var position = 0; position < model.CaseAccessorNames.Length; position++)
        {
            var accessorName = model.CaseAccessorNames[position];
            if (
                accessorName is not null
                && OutcomeMemberEmitter.ReservedMemberNames.Contains(accessorName, StringComparer.Ordinal)
            )
            {
                Report(
                    model,
                    context,
                    accessorName,
                    $"case type '{model.CaseTypeDisplayStrings[position]}' (position {position + 1})'s name "
                        + $"collides with the reserved generated member '{accessorName}'"
                );
                foundCollision = true;
            }
        }

        return foundCollision;
    }

    /// <summary>Reports one <see cref="InterlocutorDiagnostics.AccessorNameCollision"/> at the union's declaration.</summary>
    private static void Report(OutcomeUnionModel model, SourceProductionContext context, string accessorName, string reason) =>
        context.ReportDiagnostic(
            Diagnostic.Create(
                InterlocutorDiagnostics.AccessorNameCollision,
                model.Location,
                model.DisplayName,
                accessorName,
                reason
            )
        );
}
