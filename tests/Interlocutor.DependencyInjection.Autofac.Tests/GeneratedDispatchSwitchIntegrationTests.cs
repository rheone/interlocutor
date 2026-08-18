namespace Interlocutor.DependencyInjection.Autofac.Tests;

using global::Autofac;
using Interlocutor.Abstractions;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Proves <see cref="ISender.Send(object, CancellationToken)"/> (ticket 09) dispatches through the compile-time
/// generated type switch (ticket 13, <see cref="IGeneratedRequestDispatcher"/>) - not reflection - when only
/// <c>AddGeneratedRegistrations</c> is used against Autofac, with ticket 09's caller-visible contract unchanged.
/// </summary>
public sealed class GeneratedDispatchSwitchIntegrationTests
{
    [Fact]
    public async Task Send_Object_Query_DispatchesThroughTheGeneratedSwitch()
    {
        await using var scope = BuildScopeWithGeneratedRegistrationsOnly();
        var sender = scope.Resolve<ISender>();

        object request = new GeneratedSwitchQuery(21);
        var outcome = await sender.Send(request, TestContext.Current.CancellationToken);

        var union = Assert.IsAssignableFrom<System.Runtime.CompilerServices.IUnion>(outcome);
        Assert.Equal(42, Assert.IsType<int>(union.Value));
    }

    [Fact]
    public async Task Send_Object_UnitCommand_DispatchesThroughTheGeneratedSwitch()
    {
        await using var scope = BuildScopeWithGeneratedRegistrationsOnly();
        var sender = scope.Resolve<ISender>();

        object request = new GeneratedSwitchVoidCommand();
        var outcome = await sender.Send(request, TestContext.Current.CancellationToken);

        var union = Assert.IsAssignableFrom<System.Runtime.CompilerServices.IUnion>(outcome);
        Assert.IsType<Success>(union.Value);
    }

    [Fact]
    public async Task Send_Object_UnregisteredQuery_ThrowsHandlerNotRegisteredExceptionNamingTheType()
    {
        await using var scope = BuildScopeWithGeneratedRegistrationsOnly();
        var sender = scope.Resolve<ISender>();

        object request = new GeneratedSwitchUnregisteredQuery();
        var ex = await Assert.ThrowsAsync<HandlerNotRegisteredException>(() =>
            sender.Send(request, TestContext.Current.CancellationToken)
        );

        Assert.Equal(typeof(GeneratedSwitchUnregisteredQuery), ex.RequestType);
        Assert.Contains(nameof(GeneratedSwitchUnregisteredQuery), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Send_Object_NotARequest_ThrowsDistinctUnsupportedRequestTypeException()
    {
        await using var scope = BuildScopeWithGeneratedRegistrationsOnly();
        var sender = scope.Resolve<ISender>();

        var ex = await Assert.ThrowsAsync<UnsupportedRequestTypeException>(() =>
            sender.Send("just a string, not a request", TestContext.Current.CancellationToken)
        );

        Assert.Equal(typeof(string), ex.RequestType);
    }

    /// <summary>
    /// Builds a lifetime scope from a builder that only calls <c>AddGeneratedRegistrations</c> - no
    /// <c>ScanAssembly</c> - so <see cref="IGeneratedRequestDispatcher"/> is the only possible source of
    /// <see cref="ISender.Send(object, CancellationToken)"/> dispatch.
    /// </summary>
    private static ILifetimeScope BuildScopeWithGeneratedRegistrationsOnly()
    {
        var containerBuilder = new ContainerBuilder();
        containerBuilder.AddInterlocutor(builder =>
            builder.AddGeneratedRegistrations(Interlocutor.Generated.InterlocutorGeneratedRegistrations.Registrations)
        );

        var container = containerBuilder.Build();
        return container.BeginLifetimeScope();
    }

    public sealed record GeneratedSwitchQuery(int Value) : IQuery<OperationOutcome<int, Failure>>;

    public sealed class GeneratedSwitchQueryHandler : IQueryHandler<GeneratedSwitchQuery, OperationOutcome<int, Failure>>
    {
        public Task<OperationOutcome<int, Failure>> Handle(GeneratedSwitchQuery query, CancellationToken ct) =>
            Task.FromResult<OperationOutcome<int, Failure>>(query.Value * 2);
    }

    public sealed record GeneratedSwitchVoidCommand : ICommand;

    public sealed class GeneratedSwitchVoidCommandHandler : IVoidCommandHandler<GeneratedSwitchVoidCommand, OperationOutcome>
    {
        public Task<OperationOutcome> Handle(GeneratedSwitchVoidCommand command, CancellationToken ct) =>
            Task.FromResult(new OperationOutcome(default(Success)));
    }

    /// <summary>A query with no handler anywhere in this compilation - proves the generated switch's
    /// "recognized shape, no handler" case throws the same named exception the reflection path does.</summary>
    public sealed record GeneratedSwitchUnregisteredQuery : IQuery<OperationOutcome<int, Failure>>;
}
