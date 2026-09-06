namespace Interlocutor.DependencyInjection.Autofac.Tests;

using global::Autofac;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Proves <see cref="ISender.SendAsync(object, CancellationToken)"/> (ticket 09) dispatches through the compile-time
/// generated type switch (ticket 13, <see cref="IGeneratedRequestDispatcher"/>) - not reflection - when only
/// <c>AddGeneratedRegistrations</c> is used against Autofac, with ticket 09's caller-visible contract unchanged.
/// </summary>
public sealed class GeneratedDispatchSwitchIntegrationTests
{
    [Fact]
    public async Task Send_Object_Query_DispatchesThroughTheGeneratedSwitch()
    {
        // Arrange
        await using var scope = BuildScopeWithGeneratedRegistrationsOnly();
        var sender = scope.Resolve<ISender>();
        object request = new GeneratedSwitchQuery(21);

        // Act
        var outcome = await sender.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        var union = Assert.IsType<System.Runtime.CompilerServices.IUnion>(outcome, exactMatch: false);
        Assert.Equal(42, Assert.IsType<int>(union.Value));
    }

    [Fact]
    public async Task Send_Object_UnitCommand_DispatchesThroughTheGeneratedSwitch()
    {
        // Arrange
        await using var scope = BuildScopeWithGeneratedRegistrationsOnly();
        var sender = scope.Resolve<ISender>();
        object request = new GeneratedSwitchBareCommand();

        // Act
        var outcome = await sender.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert - GeneratedSwitchBareCommandHandler returns the case-less Outcome, whose Value is always null
        // (see its own remarks); reaching this point without throwing already proves dispatch through the
        // generated switch reached the handler.
        var union = Assert.IsType<System.Runtime.CompilerServices.IUnion>(outcome, exactMatch: false);
        Assert.Null(union.Value);
    }

    [Fact]
    public async Task Send_Object_UnregisteredQuery_ThrowsHandlerNotRegisteredExceptionNamingTheType()
    {
        // Arrange
        await using var scope = BuildScopeWithGeneratedRegistrationsOnly();
        var sender = scope.Resolve<ISender>();
        object request = new GeneratedSwitchUnregisteredQuery();

        // Act
        var ex = await Assert.ThrowsAsync<HandlerNotRegisteredException>(() =>
            sender.SendAsync(request, TestContext.Current.CancellationToken)
        );

        // Assert
        Assert.Equal(typeof(GeneratedSwitchUnregisteredQuery), ex.RequestType);
        Assert.Contains(nameof(GeneratedSwitchUnregisteredQuery), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Send_Object_NotARequest_ThrowsDistinctUnsupportedRequestTypeException()
    {
        // Arrange
        await using var scope = BuildScopeWithGeneratedRegistrationsOnly();
        var sender = scope.Resolve<ISender>();

        // Act
        var ex = await Assert.ThrowsAsync<UnsupportedRequestTypeException>(() =>
            sender.SendAsync("just a string, not a request", TestContext.Current.CancellationToken)
        );

        // Assert
        Assert.Equal(typeof(string), ex.RequestType);
    }

    /// <summary>
    /// Builds a lifetime scope from a builder that only calls <c>AddGeneratedRegistrations</c> - no
    /// <c>ScanAssembly</c> - so <see cref="IGeneratedRequestDispatcher"/> is the only possible source of
    /// <see cref="ISender.SendAsync(object, CancellationToken)"/> dispatch.
    /// </summary>
    private static ILifetimeScope BuildScopeWithGeneratedRegistrationsOnly()
    {
        var containerBuilder = new ContainerBuilder();
        containerBuilder.AddInterlocutor(builder =>
            builder.AddGeneratedRegistrations(
                Interlocutor
                    .Generated
                    .Interlocutor_DependencyInjection_Autofac_Tests
                    .InterlocutorGeneratedRegistrations
                    .Registrations
            )
        );

        var container = containerBuilder.Build();
        return container.BeginLifetimeScope();
    }

    public sealed record GeneratedSwitchQuery(int Value) : IQuery<Outcome<int, Failure>>;

    public sealed class GeneratedSwitchQueryHandler : IQueryHandler<GeneratedSwitchQuery, Outcome<int, Failure>>
    {
        public Task<Outcome<int, Failure>> HandleAsync(GeneratedSwitchQuery query, CancellationToken ct) =>
            Task.FromResult<Outcome<int, Failure>>(query.Value * 2);
    }

    public sealed record GeneratedSwitchBareCommand : ICommand;

    // IBareCommandHandler<,> has no HandleAsync member (deliberately unsuffixed - see that interface's own
    // remarks), so this fake stays on Handle as-is.
    public sealed class GeneratedSwitchBareCommandHandler : IBareCommandHandler<GeneratedSwitchBareCommand, Outcome>
    {
        public Task<Outcome> Handle(GeneratedSwitchBareCommand command, CancellationToken ct) =>
            Task.FromResult(default(Outcome));
    }

    /// <summary>A query with no handler anywhere in this compilation - proves the generated switch's
    /// "recognized shape, no handler" case throws the same named exception the reflection path does.</summary>
#pragma warning disable INTERLOC001 // Deliberately unregistered - this type exists to exercise that diagnostic's runtime counterpart
    public sealed record GeneratedSwitchUnregisteredQuery : IQuery<Outcome<int, Failure>>;
#pragma warning restore INTERLOC001
}
