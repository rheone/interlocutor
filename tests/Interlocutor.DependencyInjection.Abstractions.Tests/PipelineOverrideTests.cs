namespace Interlocutor.DependencyInjection.Abstractions.Tests;

using Interlocutor.Abstractions.Pipeline;

/// <summary>
/// Exercises <see cref="InterlocutorPipelineBuilder.OverridePipelineFor"/> - the Tier-1 pipeline override
/// registration surface (ADR-0019) - standalone, the same way <see cref="InterlocutorPipelineBuilderTests"/>
/// exercises the rest of <see cref="InterlocutorPipelineBuilder"/>. Chain-resolution/marker-leak-prevention
/// behavior itself lives in <c>Interlocutor.Abstractions.Tests.PipelineChainResolverTests</c> and
/// <c>Interlocutor.Abstractions.Tests.MediatorTests</c> - this file covers only what the builder records.
/// </summary>
public class PipelineOverrideTests
{
    [Fact]
    public void OverridePipelineFor_AddsOneRuleToOverrideRules()
    {
        // Arrange
        var builder = new InterlocutorPipelineBuilder();

        // Act
        builder.OverridePipelineFor(t => t == typeof(SampleRequest), "sample-rule", [typeof(FirstOverrideBehavior<,>)]);

        // Assert
        var rule = Assert.Single(builder.OverrideRules);
        Assert.Equal("sample-rule", rule.Name);
        Assert.Equal([typeof(FirstOverrideBehavior<,>)], rule.BehaviorTypes);
        Assert.True(rule.Predicate(typeof(SampleRequest)));
        Assert.False(rule.Predicate(typeof(OtherRequest)));
    }

    [Fact]
    public void OverridePipelineFor_RegistersEachDeclaredBehaviorAsAnOrdinaryBehavior()
    {
        // Arrange
        var builder = new InterlocutorPipelineBuilder();

        // Act
        builder.OverridePipelineFor(
            t => t == typeof(SampleRequest),
            "sample-rule",
            [typeof(FirstOverrideBehavior<,>), typeof(SecondOverrideBehavior<,>)]
        );

        // Assert
        Assert.Equal(
            [typeof(FirstOverrideBehavior<,>), typeof(SecondOverrideBehavior<,>)],
            builder.Behaviors.Select(r => r.ImplementationType)
        );
    }

    [Fact]
    public void OverridePipelineFor_SameBehaviorTypeDeclaredAcrossTwoRules_IsRegisteredOnlyOnce()
    {
        // Arrange
        var builder = new InterlocutorPipelineBuilder();

        // Act
        builder
            .OverridePipelineFor(t => t == typeof(SampleRequest), "first-rule", [typeof(FirstOverrideBehavior<,>)])
            .OverridePipelineFor(t => t == typeof(OtherRequest), "second-rule", [typeof(FirstOverrideBehavior<,>)]);

        // Assert
        Assert.Equal(2, builder.OverrideRules.Count);
        Assert.Single(builder.Behaviors, r => r.ImplementationType == typeof(FirstOverrideBehavior<,>));
    }

    [Fact]
    public void OverridePipelineFor_MultipleCalls_AppearInCallOrder()
    {
        // Arrange
        var builder = new InterlocutorPipelineBuilder();

        // Act
        builder
            .OverridePipelineFor(t => t == typeof(SampleRequest), "first-rule", [typeof(FirstOverrideBehavior<,>)])
            .OverridePipelineFor(t => t == typeof(OtherRequest), "second-rule", [typeof(SecondOverrideBehavior<,>)]);

        // Assert
        Assert.Equal(["first-rule", "second-rule"], builder.OverrideRules.Select(r => r.Name));
    }

    [Fact]
    public void OverridePipelineFor_EmptyBehaviorList_Throws()
    {
        // Arrange
        var builder = new InterlocutorPipelineBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.OverridePipelineFor(t => t == typeof(SampleRequest), "sample-rule", []));
    }

    [Fact]
    public void OverridePipelineFor_NullPredicate_Throws()
    {
        // Arrange
        var builder = new InterlocutorPipelineBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            builder.OverridePipelineFor(null!, "sample-rule", [typeof(FirstOverrideBehavior<,>)])
        );
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void OverridePipelineFor_MissingName_Throws(string? name)
    {
        // Arrange
        var builder = new InterlocutorPipelineBuilder();

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() =>
            builder.OverridePipelineFor(t => t == typeof(SampleRequest), name!, [typeof(FirstOverrideBehavior<,>)])
        );
    }

    [Fact]
    public void OverridePipelineForGeneric_MatchesOnlyTheDeclaredRequestType()
    {
        // Arrange
        var builder = new InterlocutorPipelineBuilder();

        // Act
        builder.OverridePipelineFor<SampleRequest>("sample-rule", typeof(FirstOverrideBehavior<,>));

        // Assert
        var rule = Assert.Single(builder.OverrideRules);
        Assert.True(rule.Predicate(typeof(SampleRequest)));
        Assert.False(rule.Predicate(typeof(OtherRequest)));
    }

    [Fact]
    public void OverridePipelineForGeneric_DeclaresBehaviorsInGivenOrder()
    {
        // Arrange
        var builder = new InterlocutorPipelineBuilder();

        // Act
        builder.OverridePipelineFor<SampleRequest>(
            "sample-rule",
            typeof(FirstOverrideBehavior<,>),
            typeof(SecondOverrideBehavior<,>)
        );

        // Assert
        var rule = Assert.Single(builder.OverrideRules);
        Assert.Equal([typeof(FirstOverrideBehavior<,>), typeof(SecondOverrideBehavior<,>)], rule.BehaviorTypes);
    }

    // Deliberately empty: type-identity markers for this test's generic-constraint shape, not data holders.
#pragma warning disable S2094
    private sealed record SampleRequest;

    private sealed record OtherRequest;
#pragma warning restore S2094

    private sealed class FirstOverrideBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        public Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct) => next();
    }

    private sealed class SecondOverrideBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        public Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct) => next();
    }
}
