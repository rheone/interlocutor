namespace Interlocutor.DependencyInjection.Microsoft.Tests;

using global::Microsoft.Extensions.DependencyInjection;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Proves <see cref="ISender.SendAsync(object, CancellationToken)"/> (ticket 09) dispatches through the compile-time
/// generated type switch (ticket 13, <see cref="IGeneratedRequestDispatcher"/>) - not reflection - when only
/// <c>AddGeneratedRegistrations</c> is used, with the exact caller-visible contract ticket 09 established
/// unchanged: a query, a value-returning command, and a unit command all dispatch correctly, an unregistered
/// request type names itself in <see cref="HandlerNotRegisteredException"/>, and a non-request object produces
/// <see cref="UnsupportedRequestTypeException"/>.
/// </summary>
public sealed class GeneratedDispatchSwitchIntegrationTests
{
    [Fact]
    public async Task Send_Object_Query_DispatchesThroughTheGeneratedSwitch()
    {
        // Arrange
        var sender = CreateSenderWithGeneratedRegistrationsOnly();
        object request = new GeneratedSwitchQuery(21);

        // Act
        var outcome = await sender.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        var union = Assert.IsType<System.Runtime.CompilerServices.IUnion>(outcome, exactMatch: false);
        Assert.Equal(42, Assert.IsType<int>(union.Value));
    }

    [Fact]
    public async Task Send_Object_ValueCommand_DispatchesThroughTheGeneratedSwitch()
    {
        // Arrange
        var sender = CreateSenderWithGeneratedRegistrationsOnly();
        object request = new GeneratedSwitchCommand("Ada");

        // Act
        var outcome = await sender.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        var union = Assert.IsType<System.Runtime.CompilerServices.IUnion>(outcome, exactMatch: false);
        Assert.Equal("Hello Ada", Assert.IsType<string>(union.Value));
    }

    [Fact]
    public async Task Send_Object_UnitCommand_DispatchesThroughTheGeneratedSwitch()
    {
        // Arrange
        var sender = CreateSenderWithGeneratedRegistrationsOnly();
        object request = new GeneratedSwitchVoidCommand();

        // Act
        var outcome = await sender.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        var union = Assert.IsType<System.Runtime.CompilerServices.IUnion>(outcome, exactMatch: false);
        Assert.IsType<Success>(union.Value);
    }

    [Fact]
    public async Task Send_Object_UnregisteredQuery_ThrowsHandlerNotRegisteredExceptionNamingTheType()
    {
        // Arrange
        var sender = CreateSenderWithGeneratedRegistrationsOnly();
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
        var sender = CreateSenderWithGeneratedRegistrationsOnly();

        // Act
        var ex = await Assert.ThrowsAsync<UnsupportedRequestTypeException>(() =>
            sender.SendAsync("just a string, not a request", TestContext.Current.CancellationToken)
        );

        // Assert
        Assert.Equal(typeof(string), ex.RequestType);
    }

    /// <summary>
    /// Builds a sender from a builder that only calls <c>AddGeneratedRegistrations</c> - no
    /// <c>ScanAssembly</c> - so <see cref="IGeneratedRequestDispatcher"/> is the only possible source of
    /// <see cref="ISender.SendAsync(object, CancellationToken)"/> dispatch: if this passed via the reflection
    /// fallback instead, that would mean the generated dispatcher failed to register or resolve.
    /// </summary>
    private static ISender CreateSenderWithGeneratedRegistrationsOnly()
    {
        var services = new ServiceCollection();
        services.AddInterlocutor(builder =>
            builder.AddGeneratedRegistrations(
                Interlocutor
                    .Generated
                    .Interlocutor_DependencyInjection_Microsoft_Tests
                    .InterlocutorGeneratedRegistrations
                    .Registrations
            )
        );

        // No `using`/disposal here: this helper's scope must outlive the method for the caller's dispatch
        // call, and each test is small and self-contained enough that leaving the scope for the process to
        // reclaim is preferable to plumbing IDisposable back out through every call site. CA2000 flags this
        // as a leak because it can't see that the ISender returned below is resolved from - and needs to
        // keep using - this same scope; adding `using` here is NOT a cleanup, it disposes the scope before
        // the caller ever calls Send, and every test through this helper starts throwing
        // ObjectDisposedException the moment it does (confirmed by reintroducing it and rerunning the suite).
        var provider = services.BuildServiceProvider();
#pragma warning disable CA2000
        var scope = provider.CreateScope();
#pragma warning restore CA2000

        return scope.ServiceProvider.GetRequiredService<ISender>();
    }

    public sealed record GeneratedSwitchQuery(int Value) : IQuery<Outcome<int, Failure>>;

    public sealed class GeneratedSwitchQueryHandler : IQueryHandler<GeneratedSwitchQuery, Outcome<int, Failure>>
    {
        public Task<Outcome<int, Failure>> HandleAsync(GeneratedSwitchQuery query, CancellationToken ct) =>
            Task.FromResult<Outcome<int, Failure>>(query.Value * 2);
    }

    public sealed record GeneratedSwitchCommand(string Name) : ICommand<Outcome<string, Failure>>;

    public sealed class GeneratedSwitchCommandHandler : ICommandHandler<GeneratedSwitchCommand, Outcome<string, Failure>>
    {
        public Task<Outcome<string, Failure>> HandleAsync(GeneratedSwitchCommand command, CancellationToken ct) =>
            Task.FromResult<Outcome<string, Failure>>($"Hello {command.Name}");
    }

    public sealed record GeneratedSwitchVoidCommand : ICommand;

    // IVoidCommandHandler<,> has no HandleAsync member (known gap - ticket 01 never added one), so this fake
    // stays on Handle as-is.
    public sealed class GeneratedSwitchVoidCommandHandler : IVoidCommandHandler<GeneratedSwitchVoidCommand, Outcome>
    {
        public Task<Outcome> Handle(GeneratedSwitchVoidCommand command, CancellationToken ct) =>
            Task.FromResult(new Outcome(default(Success)));
    }

    /// <summary>A query with no handler anywhere in this compilation - proves the generated switch's
    /// "recognized shape, no handler" case throws the same named exception the reflection path does.</summary>
#pragma warning disable INTERLOC001 // Deliberately unregistered - this type exists to exercise that diagnostic's runtime counterpart
    public sealed record GeneratedSwitchUnregisteredQuery : IQuery<Outcome<int, Failure>>;
#pragma warning restore INTERLOC001
}
