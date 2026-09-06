namespace Interlocutor.Generator.Tests;

using System.Linq;
using Interlocutor.Generator.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// Tests for <see cref="OutcomeMemberGenerator"/> and <see cref="InterlocutorDiagnostics.TooFewOutcomeCaseTypes"/>,
/// proving the attribute/generator mechanism end-to-end on a two-case union.
/// </summary>
public sealed class OutcomeMemberGeneratorTests
{
    private const string AttributedTwoCaseUnion = """
        namespace Test;

        using Interlocutor.CommonKernel;
        using Interlocutor.CommonKernel.ResultTypes;

        [GenerateOutcomeMembers]
        public readonly partial union TestOutcome<TSuccess, TFailure>(TSuccess, TFailure) : IOutcome
        {
        }
        """;

    [Fact]
    public void OutcomeMemberGenerator_TwoCaseUnion_EmitsExpectedMembers_Test()
    {
        // Act
        var result = GeneratorTestHarness.RunAndUpdate(
            new OutcomeMemberGenerator(),
            AttributedTwoCaseUnion,
            "OutcomeMemberGenerator.Tests.TwoCase",
            null,
            LanguageVersion.Preview,
            out var diagnostics
        );

        // Assert
        Assert.Empty(diagnostics);
        var emitResult = result.GetDiagnostics(TestContext.Current.CancellationToken);
        Assert.Empty(emitResult.Where(static d => d.Severity == DiagnosticSeverity.Error));

        var generatedText = string.Join(
            System.Environment.NewLine,
            result
                .SyntaxTrees.Where(static t => t.FilePath.Contains("OutcomeMembers", StringComparison.Ordinal))
                .Select(static t => t.ToString())
        );
        Assert.Contains("public global::System.Type? ActiveCaseType", generatedText, StringComparison.Ordinal);
        Assert.Contains("public bool TryGetValue(out", generatedText, StringComparison.Ordinal);
        Assert.Contains("public TResult Match<TResult>(", generatedText, StringComparison.Ordinal);
        Assert.Contains("public void Switch(", generatedText, StringComparison.Ordinal);
        Assert.Contains("public override string ToString()", generatedText, StringComparison.Ordinal);
        Assert.Contains("global::System.IEquatable<TestOutcome<TSuccess, TFailure>>", generatedText, StringComparison.Ordinal);
        Assert.Contains("public bool Equals(TestOutcome<TSuccess, TFailure> other)", generatedText, StringComparison.Ordinal);
        Assert.Contains("public override bool Equals(object? obj)", generatedText, StringComparison.Ordinal);
        Assert.Contains("public override int GetHashCode()", generatedText, StringComparison.Ordinal);
        Assert.Contains("public static bool operator ==(", generatedText, StringComparison.Ordinal);
        Assert.Contains("public static bool operator !=(", generatedText, StringComparison.Ordinal);
    }

    [Fact]
    public void OutcomeMemberGenerator_FewerThanTwoCases_ReportsError_Test()
    {
        // Arrange
        const string source = """
            namespace Test;

            using Interlocutor.CommonKernel;
            using Interlocutor.CommonKernel.ResultTypes;

            [GenerateOutcomeMembers]
            public readonly partial union OneCaseOutcome<TSuccess>(TSuccess) : IOutcome
            {
            }
            """;

        // Act
        _ = GeneratorTestHarness.RunAndUpdate(
            new OutcomeMemberGenerator(),
            source,
            "OutcomeMemberGenerator.Tests.OneCase",
            null,
            LanguageVersion.Preview,
            out var diagnostics
        );

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("INTERLOC011", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [Fact]
    public void OutcomeMemberGenerator_SixteenCaseUnion_EmitsExpectedMembers_Test()
    {
        // Arrange - exactly the BCL's Func<T1..T16, TResult>/Action<T1..T16> ceiling; must still succeed
        const string source = """
            namespace Test;

            using Interlocutor.CommonKernel;
            using Interlocutor.CommonKernel.ResultTypes;

            [GenerateOutcomeMembers]
            public readonly partial union SixteenCaseOutcome<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(
                T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16
            ) : IOutcome
            {
            }
            """;

        // Act
        var result = GeneratorTestHarness.RunAndUpdate(
            new OutcomeMemberGenerator(),
            source,
            "OutcomeMemberGenerator.Tests.SixteenCase",
            null,
            LanguageVersion.Preview,
            out var diagnostics
        );

        // Assert
        Assert.Empty(diagnostics);
        var emitResult = result.GetDiagnostics(TestContext.Current.CancellationToken);
        Assert.Empty(emitResult.Where(static d => d.Severity == DiagnosticSeverity.Error));

        var generatedText = string.Join(
            System.Environment.NewLine,
            result
                .SyntaxTrees.Where(static t => t.FilePath.Contains("OutcomeMembers", StringComparison.Ordinal))
                .Select(static t => t.ToString())
        );
        Assert.Contains("public TResult Match<TResult>(", generatedText, StringComparison.Ordinal);
        Assert.Contains("public void Switch(", generatedText, StringComparison.Ordinal);
        Assert.Equal(16, CountOccurrences(generatedText, "public bool TryGetValue(out"));
    }

    [Fact]
    public void OutcomeMemberGenerator_SeventeenCases_ReportsError_Test()
    {
        // Arrange - one past the BCL's Func<T1..T16, TResult>/Action<T1..T16> ceiling
        const string source = """
            namespace Test;

            using Interlocutor.CommonKernel;
            using Interlocutor.CommonKernel.ResultTypes;

            [GenerateOutcomeMembers]
            public readonly partial union SeventeenCaseOutcome<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(
                T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17
            ) : IOutcome
            {
            }
            """;

        // Act
        _ = GeneratorTestHarness.RunAndUpdate(
            new OutcomeMemberGenerator(),
            source,
            "OutcomeMemberGenerator.Tests.SeventeenCase",
            null,
            LanguageVersion.Preview,
            out var diagnostics
        );

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("INTERLOC013", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("Func<T1..T16,TResult>", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void OutcomeMemberGenerator_TwoCaseUnion_RuntimeBehavior_Test()
    {
        // Arrange
        const string source = """
            namespace Test;

            using Interlocutor.CommonKernel;
            using Interlocutor.CommonKernel.ResultTypes;

            [GenerateOutcomeMembers]
            public readonly partial union TestOutcome<TSuccess, TFailure>(TSuccess, TFailure) : IOutcome
            {
            }

            public static class TestOutcomeFactory
            {
                public static TestOutcome<string, int> SuccessCase(string value) => value;

                public static TestOutcome<string, int> FailureCase(int value) => value;

                public static System.Type? GetActiveCaseType(TestOutcome<string, int> outcome) =>
                    ((Interlocutor.CommonKernel.ResultTypes.IOutcomeCase)outcome).ActiveCaseType;

                public static string ToText(TestOutcome<string, int> outcome) => outcome.ToString();

                public static string MatchToText(TestOutcome<string, int> outcome) =>
                    outcome.Match(s => "success:" + s, i => "failure:" + i);

                public static string SwitchToText(TestOutcome<string, int> outcome)
                {
                    string result = "unset";
                    outcome.Switch(s => result = "success:" + s, i => result = "failure:" + i);
                    return result;
                }

                public static bool TryGetSuccess(TestOutcome<string, int> outcome, out string value) =>
                    outcome.TryGetValue(out value);

                public static bool TryGetFailure(TestOutcome<string, int> outcome, out int value) =>
                    outcome.TryGetValue(out value);

                public static bool ObjectEquals(TestOutcome<string, int> left, object? right) => left.Equals(right);

                public static bool TypedEquals(TestOutcome<string, int> left, TestOutcome<string, int> right) =>
                    left.Equals(right);

                public static bool OperatorEquals(TestOutcome<string, int> left, TestOutcome<string, int> right) =>
                    left == right;

                public static bool OperatorNotEquals(TestOutcome<string, int> left, TestOutcome<string, int> right) =>
                    left != right;

                public static int HashCode(TestOutcome<string, int> outcome) => outcome.GetHashCode();
            }
            """;

        var (assembly, _) = GeneratorTestHarness.EmitAndLoadAssembly(
            new OutcomeMemberGenerator(),
            source,
            "OutcomeMemberGenerator.Tests.RuntimeBehavior",
            null,
            LanguageVersion.Preview
        );

        var factory = assembly.GetType("Test.TestOutcomeFactory", throwOnError: true)!;
        var success = factory.GetMethod("SuccessCase")!.Invoke(null, ["hello"]);
        var failure = factory.GetMethod("FailureCase")!.Invoke(null, [42]);

        // Act & Assert - ActiveCaseType
        Assert.Equal(typeof(string), (Type?)factory.GetMethod("GetActiveCaseType")!.Invoke(null, [success]));
        Assert.Equal(typeof(int), (Type?)factory.GetMethod("GetActiveCaseType")!.Invoke(null, [failure]));

        // Act & Assert - ToString
        Assert.Equal("hello", factory.GetMethod("ToText")!.Invoke(null, [success]));
        Assert.Equal("42", factory.GetMethod("ToText")!.Invoke(null, [failure]));

        // Act & Assert - Match
        Assert.Equal("success:hello", factory.GetMethod("MatchToText")!.Invoke(null, [success]));
        Assert.Equal("failure:42", factory.GetMethod("MatchToText")!.Invoke(null, [failure]));

        // Act & Assert - Switch
        Assert.Equal("success:hello", factory.GetMethod("SwitchToText")!.Invoke(null, [success]));
        Assert.Equal("failure:42", factory.GetMethod("SwitchToText")!.Invoke(null, [failure]));

        // Act & Assert - TryGetValue: hit and miss, per case type
        var trySuccessArgsOnSuccess = new object?[] { success, null };
        Assert.True((bool)factory.GetMethod("TryGetSuccess")!.Invoke(null, trySuccessArgsOnSuccess)!);
        Assert.Equal("hello", trySuccessArgsOnSuccess[1]);

        var trySuccessArgsOnFailure = new object?[] { failure, null };
        Assert.False((bool)factory.GetMethod("TryGetSuccess")!.Invoke(null, trySuccessArgsOnFailure)!);
        Assert.Null(trySuccessArgsOnFailure[1]);

        var tryFailureArgsOnFailure = new object?[] { failure, 0 };
        Assert.True((bool)factory.GetMethod("TryGetFailure")!.Invoke(null, tryFailureArgsOnFailure)!);
        Assert.Equal(42, tryFailureArgsOnFailure[1]);

        var tryFailureArgsOnSuccess = new object?[] { success, 0 };
        Assert.False((bool)factory.GetMethod("TryGetFailure")!.Invoke(null, tryFailureArgsOnSuccess)!);
        Assert.Equal(0, tryFailureArgsOnSuccess[1]);

        // Act & Assert - equality: same case value, equal hash codes
        var successAgain = factory.GetMethod("SuccessCase")!.Invoke(null, ["hello"]);
        Assert.True((bool)factory.GetMethod("TypedEquals")!.Invoke(null, [success, successAgain])!);
        Assert.True((bool)factory.GetMethod("ObjectEquals")!.Invoke(null, [success, successAgain])!);
        Assert.True((bool)factory.GetMethod("OperatorEquals")!.Invoke(null, [success, successAgain])!);
        Assert.False((bool)factory.GetMethod("OperatorNotEquals")!.Invoke(null, [success, successAgain])!);
        Assert.Equal(
            factory.GetMethod("HashCode")!.Invoke(null, [success]),
            factory.GetMethod("HashCode")!.Invoke(null, [successAgain])
        );

        // Act & Assert - equality: different case values are unequal
        Assert.False((bool)factory.GetMethod("TypedEquals")!.Invoke(null, [success, failure])!);
        Assert.True((bool)factory.GetMethod("OperatorNotEquals")!.Invoke(null, [success, failure])!);

        // Act & Assert - Equals(object?) against an unrelated type is false, never throws
        Assert.False((bool)factory.GetMethod("ObjectEquals")!.Invoke(null, [success, "not an outcome"])!);
    }

    [Fact]
    public void OutcomeMemberGenerator_UnionWithoutIOutcome_OmitsClassificationSurface_Test()
    {
        // Arrange - no `: IOutcome` in the base list, unlike AttributedTwoCaseUnion above
        const string source = """
            namespace Test;

            using Interlocutor.CommonKernel;

            [GenerateOutcomeMembers]
            public readonly partial union PlainUnion<TFirst, TSecond>(TFirst, TSecond)
            {
            }
            """;

        // Act
        var result = GeneratorTestHarness.RunAndUpdate(
            new OutcomeMemberGenerator(),
            source,
            "OutcomeMemberGenerator.Tests.PlainUnion",
            null,
            LanguageVersion.Preview,
            out var diagnostics
        );

        // Assert
        Assert.Empty(diagnostics);
        var emitResult = result.GetDiagnostics(TestContext.Current.CancellationToken);
        Assert.Empty(emitResult.Where(static d => d.Severity == DiagnosticSeverity.Error));

        var generatedText = string.Join(
            System.Environment.NewLine,
            result
                .SyntaxTrees.Where(static t => t.FilePath.Contains("OutcomeMembers", StringComparison.Ordinal))
                .Select(static t => t.ToString())
        );

        // The sum-type surface is unconditional.
        Assert.Contains("public bool TryGetValue(out", generatedText, StringComparison.Ordinal);
        Assert.Contains("public TResult Match<TResult>(", generatedText, StringComparison.Ordinal);
        Assert.Contains("public void Switch(", generatedText, StringComparison.Ordinal);
        Assert.Contains("public override string ToString()", generatedText, StringComparison.Ordinal);
        Assert.Contains("public bool Equals(PlainUnion<TFirst, TSecond> other)", generatedText, StringComparison.Ordinal);

        // The identity surface is opt-in via `: IOutcome` and must not appear here.
        Assert.DoesNotContain("IOutcomeCase", generatedText, StringComparison.Ordinal);
        Assert.DoesNotContain("ActiveCaseType", generatedText, StringComparison.Ordinal);
    }

    [Fact]
    public void OutcomeMemberGenerator_UnionWithoutIOutcome_NeverImplementsIOutcomeCase_AtRuntime_Test()
    {
        // Arrange
        const string source = """
            namespace Test;

            using Interlocutor.CommonKernel;

            [GenerateOutcomeMembers]
            public readonly partial union PlainUnion<TFirst, TSecond>(TFirst, TSecond)
            {
            }
            """;

        // Act
        var (assembly, _) = GeneratorTestHarness.EmitAndLoadAssembly(
            new OutcomeMemberGenerator(),
            source,
            "OutcomeMemberGenerator.Tests.PlainUnionRuntime",
            null,
            LanguageVersion.Preview
        );

        // Assert - never enters the outcome case registry's IOutcomeCase-based active-case lookup, since
        // that path is reached only through an IOutcomeCase cast (see OutcomeCaseExtensions.AsOutcomeCaseActiveType).
        var plainUnionType = assembly.GetType("Test.PlainUnion`2", throwOnError: true)!;
        Assert.DoesNotContain(
            plainUnionType.GetInterfaces(),
            i => i.FullName == "Interlocutor.CommonKernel.ResultTypes.IOutcomeCase"
        );
    }

    [Fact]
    public void OutcomeMemberGenerator_ConcreteCaseTypes_EmitsNamedAccessorsAlongsideAnonymousOverload_Test()
    {
        // Arrange - every case type is concrete, none is the union's own type parameter
        const string source = """
            namespace Test;

            using Interlocutor.CommonKernel;
            using Interlocutor.CommonKernel.ResultTypes;

            [GenerateOutcomeMembers]
            public readonly partial union ConcreteCasesOutcome(Success, NotFound, Conflict) : IOutcome
            {
            }
            """;

        // Act
        var result = GeneratorTestHarness.RunAndUpdate(
            new OutcomeMemberGenerator(),
            source,
            "OutcomeMemberGenerator.Tests.ConcreteCases",
            null,
            LanguageVersion.Preview,
            out var diagnostics
        );

        // Assert
        Assert.Empty(diagnostics);
        var emitResult = result.GetDiagnostics(TestContext.Current.CancellationToken);
        Assert.Empty(emitResult.Where(static d => d.Severity == DiagnosticSeverity.Error));

        var generatedText = string.Join(
            System.Environment.NewLine,
            result
                .SyntaxTrees.Where(static t => t.FilePath.Contains("OutcomeMembers", StringComparison.Ordinal))
                .Select(static t => t.ToString())
        );

        // Named accessors, additive alongside the anonymous overload, for every concrete case.
        Assert.Contains("public bool TryGetSuccess(out", generatedText, StringComparison.Ordinal);
        Assert.Contains("public bool TryGetNotFound(out", generatedText, StringComparison.Ordinal);
        Assert.Contains("public bool TryGetConflict(out", generatedText, StringComparison.Ordinal);
        Assert.Equal(3, CountOccurrences(generatedText, "public bool TryGetValue(out"));
    }

    [Fact]
    public void OutcomeMemberGenerator_MixedTypeParameterAndConcreteCases_NamesOnlyTheConcreteOnes_Test()
    {
        // Arrange - TSuccess is the union's own type parameter; NotFound/Conflict are concrete
        const string source = """
            namespace Test;

            using Interlocutor.CommonKernel;
            using Interlocutor.CommonKernel.ResultTypes;

            [GenerateOutcomeMembers]
            public readonly partial union MixedResult<TSuccess>(TSuccess, NotFound, Conflict) : IOutcome
            {
            }
            """;

        // Act
        var result = GeneratorTestHarness.RunAndUpdate(
            new OutcomeMemberGenerator(),
            source,
            "OutcomeMemberGenerator.Tests.MixedCases",
            null,
            LanguageVersion.Preview,
            out var diagnostics
        );

        // Assert
        Assert.Empty(diagnostics);
        var emitResult = result.GetDiagnostics(TestContext.Current.CancellationToken);
        Assert.Empty(emitResult.Where(static d => d.Severity == DiagnosticSeverity.Error));

        var generatedText = string.Join(
            System.Environment.NewLine,
            result
                .SyntaxTrees.Where(static t => t.FilePath.Contains("OutcomeMembers", StringComparison.Ordinal))
                .Select(static t => t.ToString())
        );

        Assert.Contains("public bool TryGetNotFound(out", generatedText, StringComparison.Ordinal);
        Assert.Contains("public bool TryGetConflict(out", generatedText, StringComparison.Ordinal);
        Assert.DoesNotContain("TryGetTSuccess(", generatedText, StringComparison.Ordinal);

        // Every case still gets the anonymous overload, including the type-parameter case.
        Assert.Equal(3, CountOccurrences(generatedText, "public bool TryGetValue(out"));
    }

    [Fact]
    public void OutcomeMemberGenerator_ShippedFamilyShape_EveryCaseIsATypeParameter_EmitsNoNamedAccessors_Test()
    {
        // Arrange - mirrors the shipped Outcome<T1, T2, ...> family: every case type at the
        // declaration site is the union's own type parameter, so none should get a named accessor.
        // Act
        var result = GeneratorTestHarness.RunAndUpdate(
            new OutcomeMemberGenerator(),
            AttributedTwoCaseUnion,
            "OutcomeMemberGenerator.Tests.ShippedFamilyShape",
            null,
            LanguageVersion.Preview,
            out var diagnostics
        );

        // Assert
        Assert.Empty(diagnostics);

        var generatedText = string.Join(
            System.Environment.NewLine,
            result
                .SyntaxTrees.Where(static t => t.FilePath.Contains("OutcomeMembers", StringComparison.Ordinal))
                .Select(static t => t.ToString())
        );

        Assert.DoesNotContain("TryGetTSuccess(", generatedText, StringComparison.Ordinal);
        Assert.DoesNotContain("TryGetTFailure(", generatedText, StringComparison.Ordinal);
        Assert.Equal(2, CountOccurrences(generatedText, "public bool TryGetValue(out"));
    }

    [Fact]
    public void OutcomeMemberGenerator_NamedAccessor_RuntimeBehavior_Test()
    {
        // Arrange
        const string source = """
            namespace Test;

            using Interlocutor.CommonKernel;
            using Interlocutor.CommonKernel.ResultTypes;

            [GenerateOutcomeMembers]
            public readonly partial union ConcreteCasesOutcome(Success, NotFound, Conflict) : IOutcome
            {
            }

            public static class ConcreteCasesOutcomeFactory
            {
                public static ConcreteCasesOutcome SuccessCase() => new Success();

                public static ConcreteCasesOutcome NotFoundCase() => new NotFound("missing");

                public static bool TryGetNotFound(ConcreteCasesOutcome outcome, out NotFound value) =>
                    outcome.TryGetNotFound(out value);

                public static bool TryGetSuccess(ConcreteCasesOutcome outcome, out Success value) =>
                    outcome.TryGetSuccess(out value);
            }
            """;

        var (assembly, _) = GeneratorTestHarness.EmitAndLoadAssembly(
            new OutcomeMemberGenerator(),
            source,
            "OutcomeMemberGenerator.Tests.NamedAccessorRuntime",
            null,
            LanguageVersion.Preview
        );

        var factory = assembly.GetType("Test.ConcreteCasesOutcomeFactory", throwOnError: true)!;
        var success = factory.GetMethod("SuccessCase")!.Invoke(null, null);
        var notFound = factory.GetMethod("NotFoundCase")!.Invoke(null, null);

        // Act & Assert - the named accessor hits, and populates the case value, only when its own case is
        // active. NotFound/Success are both value types, so a "miss" out value is a default struct, not null -
        // the Message field distinguishes a real hit from that default.
        var notFoundArgsOnNotFound = new object?[] { notFound, null };
        Assert.True((bool)factory.GetMethod("TryGetNotFound")!.Invoke(null, notFoundArgsOnNotFound)!);
        Assert.Equal(
            "missing",
            notFoundArgsOnNotFound[1]!.GetType().GetProperty("Message")!.GetValue(notFoundArgsOnNotFound[1])
        );

        var notFoundArgsOnSuccess = new object?[] { success, null };
        Assert.False((bool)factory.GetMethod("TryGetNotFound")!.Invoke(null, notFoundArgsOnSuccess)!);
        Assert.Null(notFoundArgsOnSuccess[1]!.GetType().GetProperty("Message")!.GetValue(notFoundArgsOnSuccess[1]));

        var successArgsOnSuccess = new object?[] { success, null };
        Assert.True((bool)factory.GetMethod("TryGetSuccess")!.Invoke(null, successArgsOnSuccess)!);
    }

    /// <summary>Counts non-overlapping occurrences of <paramref name="value"/> in <paramref name="text"/>.</summary>
    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    [Fact]
    public void OutcomeMemberGenerator_PeerCaseTypesWithSameSimpleName_ReportsAccessorCollision_Test()
    {
        // Arrange - Http.NotFound and Domain.NotFound would both want TryGetNotFound
        const string source = """
            namespace Test.Http
            {
                public readonly record struct NotFound;
            }

            namespace Test.Domain
            {
                public readonly record struct NotFound;
            }

            namespace Test
            {
                using Interlocutor.CommonKernel;
                using Interlocutor.CommonKernel.ResultTypes;

                [GenerateOutcomeMembers]
                public readonly partial union ConfusingOutcome(Success, Http.NotFound, Domain.NotFound) : IOutcome
                {
                }
            }
            """;

        // Act
        _ = GeneratorTestHarness.RunAndUpdate(
            new OutcomeMemberGenerator(),
            source,
            "OutcomeMemberGenerator.Tests.SameSimpleNameCollision",
            null,
            LanguageVersion.Preview,
            out var diagnostics
        );

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("INTERLOC012", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("TryGetNotFound", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void OutcomeMemberGenerator_ClosedGenericCaseTypesOfTheSameOpenGeneric_ReportsAccessorCollision_Test()
    {
        // Arrange - ValidationError<string> and ValidationError<int> would both want TryGetValidationError
        const string source = """
            namespace Test;

            using Interlocutor.CommonKernel;
            using Interlocutor.CommonKernel.ResultTypes;

            [GenerateOutcomeMembers]
            public readonly partial union ConfusingOutcome(Success, ValidationError<string>, ValidationError<int>) : IOutcome
            {
            }
            """;

        // Act
        _ = GeneratorTestHarness.RunAndUpdate(
            new OutcomeMemberGenerator(),
            source,
            "OutcomeMemberGenerator.Tests.ClosedGenericCollision",
            null,
            LanguageVersion.Preview,
            out var diagnostics
        );

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("INTERLOC012", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("TryGetValidationError", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void OutcomeMemberGenerator_CaseTypeNameCollidesWithReservedMemberName_ReportsAccessorCollision_Test()
    {
        // Arrange - a case type literally named "Match" is flagged defensively: its own bare name matches an
        // already-reserved generated member name, even though the TryGet-prefixed accessor scheme this
        // specific case would produce ("TryGetMatch") wouldn't itself collide with the real Match<TResult>
        // method - see InterlocutorDiagnostics.AccessorNameCollision's remarks for why all nine reserved names
        // are guarded uniformly rather than only the one ("Value" -> "TryGetValue") that literally can collide.
        const string source = """
            namespace Test;

            using Interlocutor.CommonKernel;
            using Interlocutor.CommonKernel.ResultTypes;

            public readonly record struct Match;

            [GenerateOutcomeMembers]
            public readonly partial union ConfusingOutcome(Success, Match) : IOutcome
            {
            }
            """;

        // Act
        _ = GeneratorTestHarness.RunAndUpdate(
            new OutcomeMemberGenerator(),
            source,
            "OutcomeMemberGenerator.Tests.ReservedNameCollision",
            null,
            LanguageVersion.Preview,
            out var diagnostics
        );

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("INTERLOC012", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("reserved generated member", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void OutcomeMemberGenerator_NoCollidingAccessorNames_ReportsNoDiagnosticAndKeepsNamedAccessors_Test()
    {
        // Arrange - PaymentOutcome's own shape: seven distinct concrete case types, no collision
        const string source = """
            namespace Test;

            using Interlocutor.CommonKernel;
            using Interlocutor.CommonKernel.ResultTypes;

            [GenerateOutcomeMembers]
            public readonly partial union PaymentLikeOutcome(Success, NotFound, Conflict) : IOutcome
            {
            }
            """;

        // Act
        var result = GeneratorTestHarness.RunAndUpdate(
            new OutcomeMemberGenerator(),
            source,
            "OutcomeMemberGenerator.Tests.NoCollision",
            null,
            LanguageVersion.Preview,
            out var diagnostics
        );

        // Assert
        Assert.Empty(diagnostics);
        var emitResult = result.GetDiagnostics(TestContext.Current.CancellationToken);
        Assert.Empty(emitResult.Where(static d => d.Severity == DiagnosticSeverity.Error));

        var generatedText = string.Join(
            System.Environment.NewLine,
            result
                .SyntaxTrees.Where(static t => t.FilePath.Contains("OutcomeMembers", StringComparison.Ordinal))
                .Select(static t => t.ToString())
        );
        Assert.Contains("public bool TryGetNotFound(out", generatedText, StringComparison.Ordinal);
        Assert.Contains("public bool TryGetConflict(out", generatedText, StringComparison.Ordinal);
    }
}
