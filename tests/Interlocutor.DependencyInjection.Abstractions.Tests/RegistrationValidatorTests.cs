namespace Interlocutor.DependencyInjection.Abstractions.Tests;

using Interlocutor.Abstractions.Pipeline;
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
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder.ScanAssembly(typeof(RegistrationValidatorTests).Assembly);

        // Assert
        Assert.Empty(builder.DescribeRegistrations().Problems);
    }

    [Fact]
    public void ValidateOnBuild_DetectsAnUnhandledRequestType()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder.ScanAssembly(typeof(RegistrationValidatorTests).Assembly).ValidateOnBuild();

        // Assert
        Assert.Contains(
            builder.DescribeRegistrations().Problems,
            p => p.Kind == RegistrationProblemKind.UnhandledRequestType && p.Description.Contains(nameof(UnhandledQuery))
        );
    }

    [Fact]
    public void ValidateOnBuild_WithAHandlerRegistered_HasNoUnhandledRequestTypeProblem()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder
            .AddHandler(typeof(HandledByValidateOnBuildTestQueryHandler))
            .ScanAssembly(typeof(RegistrationValidatorTests).Assembly)
            .ValidateOnBuild();

        // Assert
        Assert.DoesNotContain(
            builder.DescribeRegistrations().Problems,
            p => p.Description.Contains(nameof(HandledByValidateOnBuildTestQuery))
        );
    }

    [Fact]
    public void ValidateOnBuild_DetectsAnUnmatchedRequiredTypeMatchRule()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder
            .ScanAssembly(typeof(RegistrationValidatorTests).Assembly)
            .AddTypeMatch(t => t.Name == "NoSuchType", TypeMatchTarget.Handler, require: true)
            .ValidateOnBuild();

        // Assert
        Assert.Contains(builder.DescribeRegistrations().Problems, p => p.Kind == RegistrationProblemKind.UnmatchedRequiredRule);
    }

    [Fact]
    public void ValidateOnBuild_SatisfiedRequiredTypeMatchRule_HasNoUnmatchedRuleProblem()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder
            .ScanAssembly(typeof(RegistrationValidatorTests).Assembly)
            .AddTypeMatch(t => t == typeof(SampleQueryHandler), TypeMatchTarget.Handler, require: true)
            .ValidateOnBuild();

        // Assert
        Assert.DoesNotContain(
            builder.DescribeRegistrations().Problems,
            p => p.Kind == RegistrationProblemKind.UnmatchedRequiredRule
        );
    }

    [Fact]
    public void ValidateOnBuild_DetectsConflictingLifetimesForTheSameServiceAndImplementationPair()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder
            .AddHandler(typeof(SampleQueryHandler), RegistrationLifetime.Scoped)
            .AddTypeMatch(t => t == typeof(SampleQueryHandler), TypeMatchTarget.Handler, RegistrationLifetime.Singleton)
            .ScanAssembly(typeof(RegistrationValidatorTests).Assembly)
            .ValidateOnBuild();

        // Assert
        var problem = Assert.Single(
            builder.DescribeRegistrations().Problems,
            p => p.Kind == RegistrationProblemKind.ConflictingLifetime
        );
        Assert.Contains(nameof(SampleQueryHandler), problem.Description);
    }

    [Fact]
    public void ValidateOnBuild_SameServiceAndImplementationAtTheSameLifetime_IsNotAConflict()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder
            .AddTypeMatch(t => t == typeof(SampleQueryHandler), TypeMatchTarget.Handler)
            .AddTypeMatch(t => t == typeof(SampleQueryHandler), TypeMatchTarget.Handler)
            .ScanAssembly(typeof(RegistrationValidatorTests).Assembly)
            .ValidateOnBuild();

        // Assert
        Assert.DoesNotContain(
            builder.DescribeRegistrations().Problems,
            p => p.Kind == RegistrationProblemKind.ConflictingLifetime
        );
    }

    [Fact]
    public void ValidateOnBuild_DetectsTwoOverrideRulesMatchingTheSameRequestType()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder
            .ScanAssembly(typeof(RegistrationValidatorTests).Assembly)
            .Pipeline.OverridePipelineFor<OverriddenQuery>("first-rule", typeof(FirstOverrideBehavior<,>))
            .OverridePipelineFor<OverriddenQuery>("second-rule", typeof(SecondOverrideBehavior<,>));
        builder.ValidateOnBuild();

        // Assert
        var problem = Assert.Single(
            builder.DescribeRegistrations().Problems,
            p => p.Kind == RegistrationProblemKind.ConflictingPipelineOverride
        );
        Assert.Contains(nameof(OverriddenQuery), problem.Description);
        Assert.Contains("first-rule", problem.Description);
        Assert.Contains("second-rule", problem.Description);
    }

    [Fact]
    public void ValidateOnBuild_SingleOverrideRuleMatch_HasNoConflictingPipelineOverrideProblem()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder
            .ScanAssembly(typeof(RegistrationValidatorTests).Assembly)
            .Pipeline.OverridePipelineFor<OverriddenQuery>("only-rule", typeof(FirstOverrideBehavior<,>));
        builder.ValidateOnBuild();

        // Assert
        Assert.DoesNotContain(
            builder.DescribeRegistrations().Problems,
            p => p.Kind == RegistrationProblemKind.ConflictingPipelineOverride
        );
    }

    [Fact]
    public void ValidateOnBuild_AggregatesEveryProblemIntoOneReport_NotJustTheFirst()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder
            .ScanAssembly(typeof(RegistrationValidatorTests).Assembly)
            .AddHandler(typeof(SampleQueryHandler), RegistrationLifetime.Scoped)
            .AddTypeMatch(t => t == typeof(SampleQueryHandler), TypeMatchTarget.Handler, RegistrationLifetime.Singleton)
            .AddTypeMatch(t => t.Name == "NoSuchType", TypeMatchTarget.Handler, require: true)
            .ValidateOnBuild();

        // Assert
        var problems = builder.DescribeRegistrations().Problems;
        Assert.Contains(problems, p => p.Kind == RegistrationProblemKind.UnhandledRequestType);
        Assert.Contains(problems, p => p.Kind == RegistrationProblemKind.UnmatchedRequiredRule);
        Assert.Contains(problems, p => p.Kind == RegistrationProblemKind.ConflictingLifetime);
    }

    private sealed record SampleQuery : IQuery<Outcome>;

    private sealed record UnhandledQuery : IQuery<Outcome>;

    // Uniquely named (unlike SampleQuery/SampleQueryHandler, which several sibling test classes in this
    // shared assembly also declare with the same simple name) so ValidateOnBuild_WithAHandlerRegistered_...'s
    // description-text assertion can't accidentally match another class's unrelated request type.
    private sealed record HandledByValidateOnBuildTestQuery : IQuery<Outcome>;

    private sealed class HandledByValidateOnBuildTestQueryHandler : IQueryHandler<HandledByValidateOnBuildTestQuery, Outcome>
    {
        public Task<Outcome> HandleAsync(HandledByValidateOnBuildTestQuery query, CancellationToken ct) =>
            Task.FromResult(default(Outcome));
    }

    private sealed class SampleQueryHandler : IQueryHandler<SampleQuery, Outcome>
    {
        public Task<Outcome> HandleAsync(SampleQuery query, CancellationToken ct) => Task.FromResult(default(Outcome));
    }

    private sealed record OverriddenQuery : IQuery<Outcome>;

#pragma warning disable S1144 // Resolved only via AddInterlocutor()'s assembly scanning, invisible to static analysis.
    private sealed class OverriddenQueryHandler : IQueryHandler<OverriddenQuery, Outcome>
    {
        public Task<Outcome> HandleAsync(OverriddenQuery query, CancellationToken ct) => Task.FromResult(default(Outcome));
    }
#pragma warning restore S1144

    private sealed class FirstOverrideBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        public Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct) => next();
    }

    private sealed class SecondOverrideBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        public Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct) => next();
    }
}
