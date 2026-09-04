namespace Interlocutor.Generator.Tests.Emission;

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Guards ticket 08 (single source of truth for the Outcome generator's reserved accessor names):
/// <c>Interlocutor.Generator.Analysis.OutcomeAccessorCollisionAnalyzer</c> no longer hand-declares its own
/// reserved-member-name list - it reads <c>Interlocutor.Generator.Emission.OutcomeMemberEmitter.ReservedMemberNames</c>
/// instead. This proves that shared set is actually complete: every fixed, case-type-independent member name
/// <see cref="OutcomeMemberGenerator"/> can emit onto a union's declaration is present in it, so a new fixed
/// member added to the emitter without updating the reserved set fails this test instead of silently escaping
/// INTERLOC012's collision check.
/// </summary>
public sealed class OutcomeMemberEmitterReservedNamesTests
{
    /// <summary>
    /// A union declared <c>: IOutcome</c>, so the emitter writes both the unconditional sum-type surface
    /// (<c>Match</c>, <c>Switch</c>, <c>TryGetValue</c>, <c>ToString</c>, equality) and the IOutcome-gated
    /// classification surface (<c>IsSuccess</c>, <c>ActiveCaseType</c>). Case types are built-in ones whose
    /// simple names ("Success", "NotFound", "Conflict") don't collide with any fixed member name, so their
    /// generated <c>TryGet{Name}</c> accessors stay trivially distinguishable from the fixed names this test
    /// checks against the reserved set.
    /// </summary>
    private const string ThreeCaseUnionImplementingIOutcome = """
        namespace Test;

        using Interlocutor.CommonKernel;
        using Interlocutor.CommonKernel.ResultTypes;

        [GenerateOutcomeMembers]
        public readonly partial union ReservedNameCoverageOutcome(Success, NotFound, Conflict) : IOutcome
        {
        }
        """;

    [Fact]
    public void ReservedMemberNames_CoversEveryFixedMemberNameTheEmitterGenerates_Test()
    {
        // Act
        var result = GeneratorTestHarness.RunAndUpdate(
            new OutcomeMemberGenerator(),
            ThreeCaseUnionImplementingIOutcome,
            "OutcomeMemberEmitter.Tests.ReservedNameCoverage",
            null,
            LanguageVersion.Preview,
            out var diagnostics
        );

        Assert.Empty(diagnostics);

        var generatedTree = result.SyntaxTrees.Single(static tree =>
            tree.FilePath.Contains("OutcomeMembers", StringComparison.Ordinal)
        );
        var typeDeclaration = generatedTree
            .GetRoot(TestContext.Current.CancellationToken)
            .DescendantNodes()
            .OfType<TypeDeclarationSyntax>()
            .Single();

        // The named per-case accessors this specific union produces are case-dependent by design, not fixed -
        // they're covered by INTERLOC012's other (peer-collision) check, not the reserved-name one under test.
        var namedCaseAccessors = new[] { "TryGetSuccess", "TryGetNotFound", "TryGetConflict" };

        var fixedMemberNames = typeDeclaration
            .Members.SelectMany(static member =>
                member switch
                {
                    MethodDeclarationSyntax method => new[] { method.Identifier.Text },
                    PropertyDeclarationSyntax property => new[] { property.Identifier.Text },
                    _ => Array.Empty<string>(),
                }
            )
            // GetActiveCaseIndex is a private helper whose name is unrelated to any case type's own simple
            // name (case types never generate an accessor named after it, unprefixed or otherwise) - it never
            // was, and needn't be, part of the reserved set a case type's own name is checked against.
            .Where(name => name is not "GetActiveCaseIndex")
            .Where(name => !namedCaseAccessors.Contains(name, StringComparer.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToImmutableArray();

        // Non-empty sanity check: if this ever came back empty, the exclusions above would be hiding a broken
        // assertion (vacuously "passing") rather than proving coverage.
        Assert.NotEmpty(fixedMemberNames);

        // The reserved set lives on an internal type with no InternalsVisibleTo into this test project - read
        // it by reflection, the same way DiscoveryModelsDebuggerDisplayTests reaches the generator's other
        // internal types.
        var emitterType = typeof(OutcomeMemberGenerator).Assembly.GetType(
            "Interlocutor.Generator.Emission.OutcomeMemberEmitter",
            throwOnError: true
        )!;
        var reservedMemberNamesProperty = emitterType.GetProperty(
            "ReservedMemberNames",
            BindingFlags.Public | BindingFlags.Static
        );
        Assert.NotNull(reservedMemberNamesProperty);
        var reservedMemberNames = (ImmutableArray<string>)reservedMemberNamesProperty.GetValue(null)!;

        // Assert - every fixed member name the emitter actually wrote is covered by the reserved set the
        // analyzer reads, so the two can no longer drift apart.
        Assert.All(fixedMemberNames, name => Assert.Contains(name, reservedMemberNames));
    }
}
