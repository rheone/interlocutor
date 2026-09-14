namespace Interlocutor.Abstractions.Tests;

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Queries;
using Interlocutor.CommonKernel.ResultTypes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// Compile-time regression coverage for the <c>IOutcome</c>-to-<c>IUnion</c> response-constraint swap: every
/// <c>where TResponse : IOutcome</c> / <c>where TResult : IOutcome</c> constraint in this package now reads
/// <c>where TResponse : System.Runtime.CompilerServices.IUnion</c> instead, so a response type that
/// implements bare <see cref="IOutcome"/> but not <c>IUnion</c> - legal before the swap - must now fail to
/// compile at every one of those sites.
/// </summary>
/// <remarks>
/// The failure under test is an ordinary C# compiler constraint-satisfaction error (no boxing/reference
/// conversion to the constraint interface), not anything a source generator reports, so this compiles a
/// small in-memory snippet directly against a <see cref="CSharpCompilation"/> referencing only the BCL and
/// this package's own already-built assemblies, and asserts on the resulting <see cref="Diagnostic"/>s.
/// </remarks>
public sealed class BareIOutcomeResponseConstraintRegressionTests
{
    // BareIOutcomeResponse implements IOutcome only - the shape that satisfied every TResponse/TResult
    // constraint in this package before this ticket. Every site the ticket's own site list names is exercised
    // below: the mediator core (ISender - Mediator's own SendAsync overloads must carry an identical
    // constraint to compile at all, per CS0425), the handler contracts (IQuery, ICommand, IQueryHandler,
    // ICommandHandler, IBareCommandHandler), every pipeline behavior (AuthorizationBehavior,
    // CancellationBehavior, TimeoutBehavior, RateLimitedBehavior), Eject, NoCommit, PipelineEjectException,
    // and the IOutcomeCaseRegistry family (IOutcomeCaseRegistry, OutcomeCaseRegistryBuilder,
    // OutcomeCaseRegistryExtensions). Every one of these now constrains its type parameter to
    // System.Runtime.CompilerServices.IUnion, which BareIOutcomeResponse does not implement, so every usage
    // below is expected to fail with CS0311/CS0314 ("cannot be used as type parameter ... there is no
    // implicit reference/boxing conversion").
    private const string Source = """
        namespace TestApp;

        using System.Threading;
        using System.Threading.Tasks;
        using Interlocutor.Abstractions.Commands;
        using Interlocutor.Abstractions.Core;
        using Interlocutor.Abstractions.Markers;
        using Interlocutor.Abstractions.Pipeline;
        using Interlocutor.Abstractions.Pipeline.Cases;
        using Interlocutor.Abstractions.Queries;
        using Interlocutor.CommonKernel.ResultTypes;

        public sealed class BareIOutcomeResponse : IOutcome;

        // --- Handler contracts ---
        public sealed class BareIOutcomeQuery : IQuery<BareIOutcomeResponse>;

        public sealed class BareIOutcomeCommand : ICommand<BareIOutcomeResponse>;

        public sealed class BareIOutcomeBareCommand : ICommand;

        public sealed class BareIOutcomeQueryHandler : IQueryHandler<BareIOutcomeQuery, BareIOutcomeResponse>
        {
            public Task<BareIOutcomeResponse> HandleAsync(BareIOutcomeQuery query, CancellationToken ct) =>
                throw new System.NotImplementedException();
        }

        public sealed class BareIOutcomeCommandHandler : ICommandHandler<BareIOutcomeCommand, BareIOutcomeResponse>
        {
            public Task<BareIOutcomeResponse> HandleAsync(BareIOutcomeCommand command, CancellationToken ct) =>
                throw new System.NotImplementedException();
        }

        public sealed class BareIOutcomeBareCommandHandler : IBareCommandHandler<BareIOutcomeBareCommand, BareIOutcomeResponse>
        {
            public Task<BareIOutcomeResponse> HandleAsync(BareIOutcomeBareCommand command, CancellationToken ct) =>
                throw new System.NotImplementedException();
        }

        // --- Mediator core: ISender's own constraint, called with an explicit type argument so this fails
        // independent of IQuery<>'s own (already-broken) declaration above. Mediator's SendAsync overloads
        // must declare an identical constraint to compile against this interface at all (CS0425), so this
        // exercises the mediator core without needing a live Mediator instance. ---
        public static class UsesSender
        {
            public static Task<BareIOutcomeResponse> Call(ISender sender) => sender.SendAsync<BareIOutcomeResponse>(default!, default);
        }

        // --- Every pipeline behavior: a closed generic field declaration alone triggers the constraint
        // check, independent of whether the behavior is ever constructed. ---
        public sealed record BareIOutcomePolicyRequest : IRequiresPolicyRequest
        {
            public string PolicyName => "x";
        }

        public sealed record BareIOutcomeCancellableRequest : ICancellableRequest;

        public sealed record BareIOutcomeTimeBoundRequest : ITimeoutRequest
        {
            public System.TimeSpan Budget => System.TimeSpan.Zero;
        }

        public sealed record BareIOutcomeThrottledRequest : IRateLimitedRequest
        {
            public string LimitKey => "x";
        }

        public sealed class UsesPipelineBehaviors
        {
            public AuthorizationBehavior<BareIOutcomePolicyRequest, BareIOutcomeResponse>? Authorization;
            public CancellationBehavior<BareIOutcomeCancellableRequest, BareIOutcomeResponse>? Cancellation;
            public TimeoutBehavior<BareIOutcomeTimeBoundRequest, BareIOutcomeResponse>? Timeout;
            public RateLimitedBehavior<BareIOutcomeThrottledRequest, BareIOutcomeResponse>? RateLimited;
        }

        // --- Eject / NoCommit / PipelineEjectException ---
        public static class UsesEject
        {
            public static BareIOutcomeResponse With(BareIOutcomeResponse response) => Eject.With(response);

            public static BareIOutcomeResponse ViaNoCommit(BareIOutcomeResponse response) => NoCommit.With(response);

            public static BareIOutcomeResponse ViaNoCommitAndEject(BareIOutcomeResponse response) => NoCommit.AndEject(response);

            public PipelineEjectException<BareIOutcomeResponse>? Exception;
        }

        // --- IOutcomeCaseRegistry / OutcomeCaseRegistryBuilder / OutcomeCaseRegistryExtensions ---
        public static class UsesRegistry
        {
            public static BareIOutcomeResponse Construct(IOutcomeCaseRegistry registry, Forbidden forbidden) =>
                registry.Construct<BareIOutcomeResponse, Forbidden>(forbidden);

            public static bool TryGetActiveCase(IOutcomeCaseRegistry registry, BareIOutcomeResponse response) =>
                registry.TryGetActiveCase<BareIOutcomeResponse>(response, out _, out _);

            public static void RegisterFactory(OutcomeCaseRegistryBuilder builder) =>
                builder.RegisterFactory<BareIOutcomeResponse, Forbidden>(static forbidden => throw new System.NotImplementedException());

            public static BareIOutcomeResponse ConstructOrThrow(IOutcomeCaseRegistry registry, Forbidden forbidden) =>
                registry.ConstructOrThrow<BareIOutcomeResponse, Forbidden>(forbidden, "Behavior", "did something", "must use a compatible response");
        }
        """;

    [Fact]
    public void ResponseImplementingBareIOutcomeButNotIUnion_FailsToCompileWithConstraintViolation()
    {
        // Arrange
        var compilation = CreateCompilation(Source);

        // Act
        var diagnostics = compilation.GetDiagnostics(TestContext.Current.CancellationToken);

        // Assert - CS0311 ("The type '...' cannot be used as type parameter ... There is no implicit
        // reference conversion ... to 'System.Runtime.CompilerServices.IUnion'.") or CS0314 (the same failure,
        // worded for a method/type-argument position). Every site named in this ticket's site list is
        // exercised above - the mediator core, every handler contract, every pipeline behavior, Eject/NoCommit/
        // PipelineEjectException, and the whole IOutcomeCaseRegistry family - so a healthy double-digit count
        // of independent errors is expected, proving the failure is the response type's own shape, not one
        // call site.
        var constraintErrors = diagnostics
            .Where(static d => d.Severity == DiagnosticSeverity.Error && (d.Id == "CS0311" || d.Id == "CS0314"))
            .ToList();

        Assert.True(
            constraintErrors.Count >= 15,
            "Expected at least 15 constraint-violation errors (one per site this ticket's site list names: "
                + "ISender, IQuery<>, ICommand<>, IQueryHandler<,>, ICommandHandler<,>, IBareCommandHandler<,>, "
                + "AuthorizationBehavior<,>, CancellationBehavior<,>, TimeoutBehavior<,>, RateLimitedBehavior<,>, "
                + "Eject, NoCommit (both members), PipelineEjectException<>, IOutcomeCaseRegistry.Construct/"
                + "TryGetActiveCase, OutcomeCaseRegistryBuilder.RegisterFactory, OutcomeCaseRegistryExtensions."
                + $"ConstructOrThrow) for a response type implementing bare IOutcome but not IUnion; got "
                + $"{constraintErrors.Count}. All diagnostics:{Environment.NewLine}"
                + string.Join(Environment.NewLine, diagnostics.Select(static d => d.ToString()))
        );
        Assert.All(
            constraintErrors,
            error => Assert.Contains("System.Runtime.CompilerServices.IUnion", error.GetMessage(), StringComparison.Ordinal)
        );
    }

    /// <summary>Builds a <see cref="CSharpCompilation"/> for <paramref name="source"/> referencing the BCL and
    /// this package's own already-built assemblies (<c>Interlocutor.Abstractions</c>,
    /// <c>Interlocutor.CommonKernel</c>) - enough to reproduce the constraint violation under test without
    /// pulling in any other Interlocutor package.</summary>
    private static CSharpCompilation CreateCompilation(string source)
    {
        var references = ImmutableArray.CreateBuilder<MetadataReference>();

        var trustedPlatformAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        if (trustedPlatformAssemblies is not null)
        {
            foreach (var path in trustedPlatformAssemblies.Split(Path.PathSeparator))
            {
                references.Add(MetadataReference.CreateFromFile(path));
            }
        }

        foreach (var assembly in new[] { typeof(IQuery<>).Assembly, typeof(IOutcome).Assembly })
        {
            references.Add(MetadataReference.CreateFromFile(assembly.Location));
        }

        var syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));
        return CSharpCompilation.Create(
            "Interlocutor.Abstractions.Tests.BareIOutcomeConstraintRegression",
            [syntaxTree],
            references.ToImmutable(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
    }
}
