namespace Interlocutor.DependencyInjection.Microsoft.Tests;

using global::Microsoft.Extensions.DependencyInjection;
using Interlocutor.Abstractions;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Proves a handler discovered by <c>Interlocutor.Generator</c> (rather than runtime assembly scanning)
/// resolves and dispatches correctly through Microsoft DI, including a constructor dependency — the
/// acceptance criteria "Both containers work against generated registrations" and "A handler with
/// constructor dependencies resolves and receives them".
/// </summary>
public sealed class GeneratedRegistrationIntegrationTests
{
    [Fact]
    public async Task GeneratedRegistration_ResolvesHandlerWithConstructorDependency_AndDispatchesSuccessfully()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IGreeter, Greeter>();

        // No ScanAssembly call: the only source of handler registrations here is the generator's own
        // compile-time discovered module — proving the generated path works on its own, not merely
        // alongside runtime scanning.
        services.AddInterlocutor(builder =>
            builder.AddGeneratedRegistrations(Interlocutor.Generated.InterlocutorGeneratedRegistrations.Registrations)
        );

        var provider = services.BuildServiceProvider();
        // Dispatch must happen from a real scope, never the root provider directly — see ADR-0011.
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new GeneratedQuery(), TestContext.Current.CancellationToken);

        Assert.Equal("hi from Microsoft DI", result.Value);
    }

    public interface IGreeter
    {
        string Greet();
    }

    public sealed record GeneratedQuery : IQuery<OperationOutcome<string, Failure>>;

    public sealed class GeneratedQueryHandler(IGreeter greeter)
        : IQueryHandler<GeneratedQuery, OperationOutcome<string, Failure>>
    {
        public Task<OperationOutcome<string, Failure>> Handle(GeneratedQuery query, CancellationToken ct) =>
            Task.FromResult<OperationOutcome<string, Failure>>(greeter.Greet());
    }

    public sealed class Greeter : IGreeter
    {
        public string Greet() => "hi from Microsoft DI";
    }
}
