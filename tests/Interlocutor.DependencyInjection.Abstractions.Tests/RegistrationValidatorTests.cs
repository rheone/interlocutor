namespace Interlocutor.DependencyInjection.Abstractions.Tests;

using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;

/// <summary>
/// Covers <see cref="InterlocutorBuilder.ValidateOnBuild"/>'s full behavioral matrix - every problem kind
/// <see cref="RegistrationValidator.ComputeProblems"/> checks, and their aggregation into one report - split off
/// from <see cref="InterlocutorBuilderTests"/> the same way <c>InterlocutorPipelineBuilderTests</c> was split off
/// under ADR-0015. Exercised entirely through the public <see cref="InterlocutorBuilder"/>/<see cref="InterlocutorBuilder.DescribeRegistrations"/>
/// surface rather than <see cref="RegistrationValidator"/> directly, since assembling the candidate-type list it
/// needs is itself part of what <see cref="InterlocutorBuilder"/> owns.
/// </summary>
public class RegistrationValidatorTests
{
    [Fact]
    public void ValidateOnBuild_NotConfigured_LeavesProblemsEmpty_EvenWithAnUnhandledRequestType()
    {
        var builder = new InterlocutorBuilder();

        builder.ScanAssembly(typeof(RegistrationValidatorTests).Assembly);

        Assert.Empty(builder.DescribeRegistrations().Problems);
    }

    [Fact]
    public void ValidateOnBuild_DetectsAnUnhandledRequestType()
    {
        var builder = new InterlocutorBuilder();

        builder.ScanAssembly(typeof(RegistrationValidatorTests).Assembly).ValidateOnBuild();

        Assert.Contains(
            builder.DescribeRegistrations().Problems,
            p => p.Kind == RegistrationProblemKind.UnhandledRequestType && p.Description.Contains(nameof(UnhandledQuery))
        );
    }

    [Fact]
    public void ValidateOnBuild_WithAHandlerRegistered_HasNoUnhandledRequestTypeProblem()
    {
        var builder = new InterlocutorBuilder();

        builder
            .AddHandler(typeof(HandledByValidateOnBuildTestQueryHandler))
            .ScanAssembly(typeof(RegistrationValidatorTests).Assembly)
            .ValidateOnBuild();

        Assert.DoesNotContain(
            builder.DescribeRegistrations().Problems,
            p => p.Description.Contains(nameof(HandledByValidateOnBuildTestQuery))
        );
    }

    [Fact]
    public void ValidateOnBuild_DetectsAnUnmatchedRequiredTypeMatchRule()
    {
        var builder = new InterlocutorBuilder();

        builder
            .ScanAssembly(typeof(RegistrationValidatorTests).Assembly)
            .AddTypeMatch(t => t.Name == "NoSuchType", TypeMatchTarget.Handler, require: true)
            .ValidateOnBuild();

        Assert.Contains(builder.DescribeRegistrations().Problems, p => p.Kind == RegistrationProblemKind.UnmatchedRequiredRule);
    }

    [Fact]
    public void ValidateOnBuild_SatisfiedRequiredTypeMatchRule_HasNoUnmatchedRuleProblem()
    {
        var builder = new InterlocutorBuilder();

        builder
            .ScanAssembly(typeof(RegistrationValidatorTests).Assembly)
            .AddTypeMatch(t => t == typeof(SampleQueryHandler), TypeMatchTarget.Handler, require: true)
            .ValidateOnBuild();

        Assert.DoesNotContain(
            builder.DescribeRegistrations().Problems,
            p => p.Kind == RegistrationProblemKind.UnmatchedRequiredRule
        );
    }

    [Fact]
    public void ValidateOnBuild_DetectsConflictingLifetimesForTheSameServiceAndImplementationPair()
    {
        var builder = new InterlocutorBuilder();

        builder
            .AddHandler(typeof(SampleQueryHandler), RegistrationLifetime.Scoped)
            .AddTypeMatch(t => t == typeof(SampleQueryHandler), TypeMatchTarget.Handler, RegistrationLifetime.Singleton)
            .ScanAssembly(typeof(RegistrationValidatorTests).Assembly)
            .ValidateOnBuild();

        var problem = Assert.Single(
            builder.DescribeRegistrations().Problems,
            p => p.Kind == RegistrationProblemKind.ConflictingLifetime
        );
        Assert.Contains(nameof(SampleQueryHandler), problem.Description);
    }

    [Fact]
    public void ValidateOnBuild_SameServiceAndImplementationAtTheSameLifetime_IsNotAConflict()
    {
        var builder = new InterlocutorBuilder();

        builder
            .AddTypeMatch(t => t == typeof(SampleQueryHandler), TypeMatchTarget.Handler)
            .AddTypeMatch(t => t == typeof(SampleQueryHandler), TypeMatchTarget.Handler)
            .ScanAssembly(typeof(RegistrationValidatorTests).Assembly)
            .ValidateOnBuild();

        Assert.DoesNotContain(
            builder.DescribeRegistrations().Problems,
            p => p.Kind == RegistrationProblemKind.ConflictingLifetime
        );
    }

    [Fact]
    public void ValidateOnBuild_AggregatesEveryProblemIntoOneReport_NotJustTheFirst()
    {
        var builder = new InterlocutorBuilder();

        builder
            .ScanAssembly(typeof(RegistrationValidatorTests).Assembly)
            .AddHandler(typeof(SampleQueryHandler), RegistrationLifetime.Scoped)
            .AddTypeMatch(t => t == typeof(SampleQueryHandler), TypeMatchTarget.Handler, RegistrationLifetime.Singleton)
            .AddTypeMatch(t => t.Name == "NoSuchType", TypeMatchTarget.Handler, require: true)
            .ValidateOnBuild();

        var problems = builder.DescribeRegistrations().Problems;
        Assert.Contains(problems, p => p.Kind == RegistrationProblemKind.UnhandledRequestType);
        Assert.Contains(problems, p => p.Kind == RegistrationProblemKind.UnmatchedRequiredRule);
        Assert.Contains(problems, p => p.Kind == RegistrationProblemKind.ConflictingLifetime);
    }

    private sealed record SampleQuery : IQuery<OperationOutcome>;

    private sealed record UnhandledQuery : IQuery<OperationOutcome>;

    // Uniquely named (unlike SampleQuery/SampleQueryHandler, which several sibling test classes in this
    // shared assembly also declare with the same simple name) so ValidateOnBuild_WithAHandlerRegistered_...'s
    // description-text assertion can't accidentally match another class's unrelated request type.
    private sealed record HandledByValidateOnBuildTestQuery : IQuery<OperationOutcome>;

    private sealed class HandledByValidateOnBuildTestQueryHandler
        : IQueryHandler<HandledByValidateOnBuildTestQuery, OperationOutcome>
    {
        public Task<OperationOutcome> Handle(HandledByValidateOnBuildTestQuery query, CancellationToken ct) =>
            Task.FromResult(default(OperationOutcome));
    }

    private sealed class SampleQueryHandler : IQueryHandler<SampleQuery, OperationOutcome>
    {
        public Task<OperationOutcome> Handle(SampleQuery query, CancellationToken ct) =>
            Task.FromResult(default(OperationOutcome));
    }
}
