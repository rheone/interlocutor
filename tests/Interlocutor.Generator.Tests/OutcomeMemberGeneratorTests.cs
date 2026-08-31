namespace Interlocutor.Generator.Tests;

using System.Linq;
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
        Assert.Contains("public bool IsSuccess", generatedText, StringComparison.Ordinal);
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

                public static bool IsSuccess(TestOutcome<string, int> outcome) => outcome.IsSuccess;

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

        // Act & Assert - IsSuccess
        Assert.True((bool)factory.GetMethod("IsSuccess")!.Invoke(null, [success])!);
        Assert.False((bool)factory.GetMethod("IsSuccess")!.Invoke(null, [failure])!);

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
}
