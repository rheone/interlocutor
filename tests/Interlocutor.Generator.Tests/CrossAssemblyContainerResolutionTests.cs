namespace Interlocutor.Generator.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using global::Autofac;
using global::Microsoft.Extensions.DependencyInjection;
using Interlocutor.Abstractions.Core;
using Interlocutor.CommonKernel;
using Interlocutor.DependencyInjection.Abstractions;
using Interlocutor.DependencyInjection.Autofac;
using Interlocutor.DependencyInjection.Microsoft;
using Microsoft.CodeAnalysis;

/// <summary>
/// Covers ticket 54's acceptance criteria: proves, with two real, loaded assemblies rather than an in-memory
/// <see cref="Microsoft.CodeAnalysis.Compilation"/> reference, that a handler declared only in a referenced
/// library resolves and dispatches correctly through both containers once the application aggregates that
/// library's generated module (ticket 12). <see cref="CrossAssemblyAggregationTests"/> already proves
/// aggregation is correct at the generated-source-text level; this closes the gap its own review left open -
/// see ticket 12's "Container coverage gap" comment - by actually emitting, loading, and dispatching through
/// both <see cref="global::Microsoft.Extensions.DependencyInjection.ServiceCollection"/> and
/// <see cref="global::Autofac.ContainerBuilder"/>.
/// </summary>
public sealed class CrossAssemblyContainerResolutionTests
{
    private const string LibrarySource = """
        namespace Library;

        using System.Threading;
        using System.Threading.Tasks;
        using Interlocutor.Abstractions.Queries;
        using Interlocutor.CommonKernel;
        using Interlocutor.CommonKernel.ResultTypes;

        public sealed record GetGreetingQuery : IQuery<OperationOutcome<string, Failure>>;

        public interface IGreeter
        {
            string Greet();
        }

        public sealed class GetGreetingQueryHandler(IGreeter greeter) : IQueryHandler<GetGreetingQuery, OperationOutcome<string, Failure>>
        {
            public Task<OperationOutcome<string, Failure>> Handle(GetGreetingQuery query, CancellationToken ct) =>
                Task.FromResult(new OperationOutcome<string, Failure>(greeter.Greet()));
        }
        """;

    // References the library's own request type (never declares a handler itself) - proving the app's
    // aggregated module resolves a handler it never saw declared anywhere in its own compilation.
    private const string AppSource = """
        namespace App;

        public static class RequestFactory
        {
            public static Library.GetGreetingQuery CreateRequest() => new Library.GetGreetingQuery();
        }
        """;

    [Fact]
    public async Task MicrosoftDependencyInjection_ResolvesAndDispatches_ToAHandlerDeclaredOnlyInTheReferencedLibrary()
    {
        var app = BuildAggregatedApp("MicrosoftDI");

        var services = new ServiceCollection();
        services.AddSingleton(app.GreeterInterfaceType, StubGreeter.Create(app.GreeterInterfaceType));
        services.AddInterlocutor(builder => builder.AddGeneratedRegistrations(app.Registrations));

        var provider = services.BuildServiceProvider();
        // Dispatch must happen from a real scope, never the root provider directly - see ADR-0011.
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var request = app.CreateRequest();
        var outcome = await mediator.Send(request, TestContext.Current.CancellationToken);

        AssertSuccessfulGreeting(outcome);
    }

    [Fact]
    public async Task Autofac_ResolvesAndDispatches_ToAHandlerDeclaredOnlyInTheReferencedLibrary()
    {
        var app = BuildAggregatedApp("Autofac");

        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterInstance(StubGreeter.Create(app.GreeterInterfaceType)).As(app.GreeterInterfaceType);
        containerBuilder.AddInterlocutor(builder => builder.AddGeneratedRegistrations(app.Registrations));

        var container = containerBuilder.Build();
        // Dispatch must happen from a real lifetime scope, never the root container directly - see ADR-0011.
        await using var scope = container.BeginLifetimeScope();
        var mediator = scope.Resolve<IMediator>();

        var request = app.CreateRequest();
        var outcome = await mediator.Send(request, TestContext.Current.CancellationToken);

        AssertSuccessfulGreeting(outcome);
    }

    /// <summary>
    /// Emits the library to a real PE and loads it, then emits the app - referencing the library's real PE
    /// bytes as a <see cref="Microsoft.CodeAnalysis.MetadataReference"/>, exactly as a genuine project
    /// reference would - and loads that too. Returns everything a test needs to build a container and
    /// dispatch: the app's aggregated <c>Registrations</c> (read by reflection, but cast to the real,
    /// already-loaded <see cref="InterlocutorRegistration"/> type - see
    /// <see cref="GeneratorTestHarness.EmitAndLoadAssembly"/>'s remarks), the library's own <c>IGreeter</c>
    /// runtime <see cref="Type"/>, and a factory for a request instance from the loaded library assembly.
    /// </summary>
    /// <param name="runId">
    /// A per-test-run-unique token folded into both emitted assemblies' names, so the two test methods in this
    /// class never collide in the shared default <see cref="System.Runtime.Loader.AssemblyLoadContext"/> - see
    /// <see cref="GeneratorTestHarness.EmitAndLoadAssembly"/>'s remarks on assembly name uniqueness.
    /// </param>
    private static AggregatedApp BuildAggregatedApp(string runId)
    {
        var (libraryAssembly, libraryPeBytes) = GeneratorTestHarness.EmitAndLoadAssembly(
            LibrarySource,
            $"Library_{runId}_{Guid.NewGuid():N}"
        );
        var libraryReference = MetadataReference.CreateFromImage(libraryPeBytes);

        var (appAssembly, _) = GeneratorTestHarness.EmitAndLoadAssembly(
            AppSource,
            $"App_{runId}_{Guid.NewGuid():N}",
            [libraryReference]
        );

        var moduleType = appAssembly
            .GetTypes()
            .Single(static t =>
                t.Name == "InterlocutorGeneratedRegistrations"
                && t.Namespace!.StartsWith("Interlocutor.Generated.", StringComparison.Ordinal)
            );
        var registrations =
            (IReadOnlyList<InterlocutorRegistration>)
                moduleType.GetProperty("Registrations", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;

        var greeterInterfaceType = libraryAssembly.GetType("Library.IGreeter")!;
        var requestFactoryMethod = appAssembly.GetType("App.RequestFactory")!.GetMethod("CreateRequest")!;

        return new AggregatedApp(registrations, greeterInterfaceType, () => requestFactoryMethod.Invoke(null, null)!);
    }

    /// <summary>
    /// <see cref="ISender.Send(object, System.Threading.CancellationToken)"/>'s runtime-typed overload
    /// returns the marker <see cref="IOperationOutcome"/> (compile-time common ground with the caller); the
    /// concrete case is read back by reflection over the boxed union value's own <c>IsSuccess</c>/<c>Value</c>
    /// members, since the caller has no compile-time reference to the library's own success/failure types.
    /// </summary>
    private static void AssertSuccessfulGreeting(IOperationOutcome outcome)
    {
        var outcomeType = outcome.GetType();
        var isSuccess = (bool)outcomeType.GetProperty("IsSuccess")!.GetValue(outcome)!;
        Assert.True(isSuccess, $"Expected a successful outcome but got {outcome}.");

        var value = outcomeType.GetProperty("Value")!.GetValue(outcome);
        Assert.Equal("hello from the cross-assembly library", value);
    }

    /// <summary>Everything one test needs from <see cref="BuildAggregatedApp"/> to build a container and dispatch a request.</summary>
    private sealed record AggregatedApp(
        IReadOnlyList<InterlocutorRegistration> Registrations,
        Type GreeterInterfaceType,
        Func<object> CreateRequest
    );

    /// <summary>
    /// A stub implementation of the library's own dynamically loaded <c>IGreeter</c> interface, built via
    /// <see cref="DispatchProxy"/> - the interface is only known at runtime (a type loaded from a generated
    /// assembly), so no compile-time type in this project can implement it directly.
    /// </summary>
    private class StubGreeter : DispatchProxy
    {
        /// <summary>Creates a proxy instance implementing <paramref name="greeterInterfaceType"/>.</summary>
        /// <param name="greeterInterfaceType">The library's dynamically loaded <c>IGreeter</c> type.</param>
        public static object Create(Type greeterInterfaceType) =>
            DispatchProxy.Create(greeterInterfaceType, typeof(StubGreeter));

        /// <inheritdoc/>
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => "hello from the cross-assembly library";
    }
}
