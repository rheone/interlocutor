namespace Interlocutor.DependencyInjection.Microsoft.Tests;

using System.Collections.Concurrent;
using System.Diagnostics;
using global::Microsoft.Extensions.DependencyInjection;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Diagnostics;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Proves ticket 05's eject-activity-tagging guarantee specifically against the compile-time generated dispatch
/// switch (ticket 13, <see cref="IGeneratedRequestDispatcher"/>), whose emitted wrapper has no
/// <see cref="Activity"/> parameter of its own and instead tags <see cref="Activity.Current"/> (see
/// <c>Interlocutor.Generator.Emission.DispatchWrapperEmitter</c>'s remarks) - the one dispatch sub-path
/// <c>Interlocutor.Abstractions.Tests.MediatorActivityTracingTests</c> cannot cover, since that class only
/// exercises the reflection-based wrappers.
/// </summary>
public sealed class GeneratedDispatchSwitchActivityTracingTests
{
    [Fact]
    public async Task Send_Object_HandlerEjects_TagsActivityAsEjected()
    {
        // Arrange
        var captured = new ConcurrentBag<Activity>();
        using var listener = CreateListener(captured);
        ActivitySource.AddActivityListener(listener);

        var sender = CreateSenderWithGeneratedRegistrationsOnly();
        object request = new GeneratedSwitchEjectingQuery();

        // Act
        await sender.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        var activity = SingleFor<GeneratedSwitchEjectingQuery>(captured);
        Assert.True((bool?)activity.GetTagItem(MediatorActivitySource.EjectedTag));
    }

    [Fact]
    public async Task Send_Object_NormalReturn_DoesNotTagActivityAsEjected()
    {
        // Arrange
        var captured = new ConcurrentBag<Activity>();
        using var listener = CreateListener(captured);
        ActivitySource.AddActivityListener(listener);

        var sender = CreateSenderWithGeneratedRegistrationsOnly();
        object request = new GeneratedSwitchNonEjectingQuery();

        // Act
        await sender.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        var activity = SingleFor<GeneratedSwitchNonEjectingQuery>(captured);
        Assert.Null(activity.GetTagItem(MediatorActivitySource.EjectedTag));
    }

    private static Activity SingleFor<TRequest>(ConcurrentBag<Activity> captured) =>
        Assert.Single(captured, a => (string?)a.GetTagItem(MediatorActivitySource.RequestTypeTag) == typeof(TRequest).FullName);

    private static ActivityListener CreateListener(ConcurrentBag<Activity> captured) =>
        new()
        {
            ShouldListenTo = source => source.Name == MediatorActivitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => captured.Add(activity),
        };

    /// <summary>
    /// Builds a sender from a builder that only calls <c>AddGeneratedRegistrations</c> - no <c>ScanAssembly</c> -
    /// so <see cref="IGeneratedRequestDispatcher"/> is the only possible source of
    /// <see cref="ISender.SendAsync(object, CancellationToken)"/> dispatch, matching
    /// <c>GeneratedDispatchSwitchIntegrationTests</c>'s identical helper.
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

        // See GeneratedDispatchSwitchIntegrationTests.CreateSenderWithGeneratedRegistrationsOnly's identical
        // remark: this scope must outlive the method for the caller's dispatch call.
        var provider = services.BuildServiceProvider();
#pragma warning disable CA2000
        var scope = provider.CreateScope();
#pragma warning restore CA2000

        return scope.ServiceProvider.GetRequiredService<ISender>();
    }

    public sealed record GeneratedSwitchEjectingQuery : IQuery<Outcome<int, Failure>>;

    public sealed class GeneratedSwitchEjectingQueryHandler : IQueryHandler<GeneratedSwitchEjectingQuery, Outcome<int, Failure>>
    {
        public Task<Outcome<int, Failure>> HandleAsync(GeneratedSwitchEjectingQuery query, CancellationToken ct) =>
            Task.FromResult(Eject.With<Outcome<int, Failure>>(new Failure("ejected")));
    }

    public sealed record GeneratedSwitchNonEjectingQuery : IQuery<Outcome<int, Failure>>;

    public sealed class GeneratedSwitchNonEjectingQueryHandler
        : IQueryHandler<GeneratedSwitchNonEjectingQuery, Outcome<int, Failure>>
    {
        public Task<Outcome<int, Failure>> HandleAsync(GeneratedSwitchNonEjectingQuery query, CancellationToken ct) =>
            Task.FromResult<Outcome<int, Failure>>(1);
    }
}
