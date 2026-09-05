namespace Interlocutor.Generator.Tests;

using Microsoft.CodeAnalysis;

/// <summary>
/// Tests for open generic query/command handler discovery and closing (ticket 11): a single generic handler
/// serving a family of generic requests, constraint honoring, closed-beats-open precedence, the
/// <c>HandlerOverrideAttribute</c> ambiguity escape hatch, and its own target validation.
/// </summary>
public sealed class OpenGenericHandlerTests
{
    private const string BasicGenericSource = """
        namespace TestApp;

        using System.Threading;
        using System.Threading.Tasks;
        using Interlocutor.Abstractions.Queries;
        using Interlocutor.CommonKernel;

        public interface IEntity;

        public sealed class Foo : IEntity
        {
            public Foo() { }
        }

        public sealed class GenericQuery<T> : IQuery<Outcome<T, string>>;

        public sealed class GenericQueryHandler<T> : IQueryHandler<GenericQuery<T>, Outcome<T, string>>
            where T : IEntity, new()
        {
            public Task<Outcome<T, string>> HandleAsync(GenericQuery<T> query, CancellationToken ct) =>
                throw new System.NotImplementedException();
        }

        public sealed class UsesFoo
        {
            public void Use()
            {
                var query = new GenericQuery<Foo>();
            }
        }
        """;

    [Fact]
    public void OpenGenericHandler_ConcreteUsage_ClosesAndProducesNoDiagnostics_Test()
    {
        // Arrange
        // (no separate setup; uses class fixtures or direct act/assert)
        // Act
        var result = GeneratorTestHarness.Run(BasicGenericSource);

        // Assert
        Assert.Empty(result.Diagnostics);

        var registrationsSource = GetGeneratedSource(result, "InterlocutorGeneratedRegistrations.g.cs");
        Assert.Contains(
            "new global::Interlocutor.DependencyInjection.Abstractions.InterlocutorRegistration(typeof(global::Interlocutor.Abstractions.Queries.IQueryHandler<global::TestApp.GenericQuery<global::TestApp.Foo>, global::Interlocutor.CommonKernel.Outcome<global::TestApp.Foo, string>>), typeof(global::TestApp.GenericQueryHandler<global::TestApp.Foo>), global::Interlocutor.DependencyInjection.Abstractions.RegistrationLifetime.Scoped, global::Interlocutor.DependencyInjection.Abstractions.RegistrationKind.Handler)",
            registrationsSource,
            StringComparison.Ordinal
        );

        var dispatchSource = GetGeneratedSource(result, "InterlocutorGeneratedDispatch.g.cs");
        Assert.Contains(
            "global::Interlocutor.Abstractions.Queries.IQueryHandler<global::TestApp.GenericQuery<global::TestApp.Foo>, global::Interlocutor.CommonKernel.Outcome<global::TestApp.Foo, string>>",
            dispatchSource,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain("new global::TestApp.GenericQueryHandler", dispatchSource, StringComparison.Ordinal);

        var switchSource = GetGeneratedSource(result, "InterlocutorGeneratedDispatchSwitch.g.cs");
        Assert.Contains(
            "case global::TestApp.GenericQuery<global::TestApp.Foo> typedRequest:",
            switchSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void GeneratedCode_ClosedGenericHandler_CompilesCleanly_Test()
    {
        // Arrange
        // (no separate setup; uses class fixtures or direct act/assert)
        // Act
        var outputCompilation = GeneratorTestHarness.RunAndUpdate(BasicGenericSource, out var generatorDiagnostics);

        // Assert
        Assert.Empty(generatorDiagnostics);

        var errors = outputCompilation
            .GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();
        Assert.Empty(errors);
    }

    [Fact]
    public void OpenGenericHandler_MultipleUsages_ClosesMultipleTimes_Test()
    {
        // Arrange
        const string source = """
            namespace TestApp;

            using System.Threading;
            using System.Threading.Tasks;
            using Interlocutor.Abstractions.Queries;
            using Interlocutor.CommonKernel;

            public interface IEntity;

            public sealed class Foo : IEntity
            {
                public Foo() { }
            }

            public sealed class Baz : IEntity
            {
                public Baz() { }
            }

            public sealed class GenericQuery<T> : IQuery<Outcome<T, string>>;

            public sealed class GenericQueryHandler<T> : IQueryHandler<GenericQuery<T>, Outcome<T, string>>
                where T : IEntity, new()
            {
                public Task<Outcome<T, string>> HandleAsync(GenericQuery<T> query, CancellationToken ct) =>
                    throw new System.NotImplementedException();
            }

            public sealed class UsesFoo
            {
                public void Use()
                {
                    var query = new GenericQuery<Foo>();
                    var other = new GenericQuery<Baz>();
                }
            }
            """;

        // Act
        var result = GeneratorTestHarness.Run(source);

        // Assert
        Assert.Empty(result.Diagnostics);

        var registrationsSource = GetGeneratedSource(result, "InterlocutorGeneratedRegistrations.g.cs");
        Assert.Contains(
            "global::TestApp.GenericQueryHandler<global::TestApp.Foo>",
            registrationsSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "global::TestApp.GenericQueryHandler<global::TestApp.Baz>",
            registrationsSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void ConstraintValidation_FailingRequest_NotClosedReportsMissing_Test()
    {
        // Arrange
        const string source = """
            namespace TestApp;

            using System.Threading;
            using System.Threading.Tasks;
            using Interlocutor.Abstractions.Queries;
            using Interlocutor.CommonKernel;

            public interface IEntity;

            // Does not implement IEntity - fails the handler's own type parameter constraint.
            public sealed class NotAnEntity
            {
                public NotAnEntity() { }
            }

            public sealed class GenericQuery<T> : IQuery<Outcome<T, string>>;

            public sealed class GenericQueryHandler<T> : IQueryHandler<GenericQuery<T>, Outcome<T, string>>
                where T : IEntity, new()
            {
                public Task<Outcome<T, string>> HandleAsync(GenericQuery<T> query, CancellationToken ct) =>
                    throw new System.NotImplementedException();
            }

            public sealed class UsesNotAnEntity
            {
                public void Use()
                {
                    var query = new GenericQuery<NotAnEntity>();
                }
            }
            """;

        // Act
        var result = GeneratorTestHarness.Run(source);

        // Assert
        Assert.Empty(result.Diagnostics);

        var registrationsSource = GetGeneratedSource(result, "InterlocutorGeneratedRegistrations.g.cs");
        Assert.DoesNotContain("NotAnEntity", registrationsSource, StringComparison.Ordinal);
    }

    [Fact]
    public void HandlerPrecedence_ClosedVsOpen_ClosedWins_Test()
    {
        // Arrange
        const string source = """
            namespace TestApp;

            using System.Threading;
            using System.Threading.Tasks;
            using Interlocutor.Abstractions.Queries;
            using Interlocutor.CommonKernel;

            public interface IEntity;

            public sealed class Foo : IEntity
            {
                public Foo() { }
            }

            public sealed class GenericQuery<T> : IQuery<Outcome<T, string>>;

            public sealed class GenericQueryHandler<T> : IQueryHandler<GenericQuery<T>, Outcome<T, string>>
                where T : IEntity, new()
            {
                public Task<Outcome<T, string>> HandleAsync(GenericQuery<T> query, CancellationToken ct) =>
                    throw new System.NotImplementedException();
            }

            // Special-cases Foo: this concrete handler must win over the open generic one above.
            public sealed class FooQueryHandler : IQueryHandler<GenericQuery<Foo>, Outcome<Foo, string>>
            {
                public Task<Outcome<Foo, string>> HandleAsync(GenericQuery<Foo> query, CancellationToken ct) =>
                    throw new System.NotImplementedException();
            }

            public sealed class UsesFoo
            {
                public void Use()
                {
                    var query = new GenericQuery<Foo>();
                }
            }
            """;

        // Act
        var result = GeneratorTestHarness.Run(source);

        // Assert
        Assert.Empty(result.Diagnostics);

        var registrationsSource = GetGeneratedSource(result, "InterlocutorGeneratedRegistrations.g.cs");
        Assert.Contains("typeof(global::TestApp.FooQueryHandler)", registrationsSource, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "global::TestApp.GenericQueryHandler<global::TestApp.Foo>",
            registrationsSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void AmbiguityDetection_MultipleOpenHandlers_ReportsNamingBoth_Test()
    {
        // Arrange
        const string source = """
            namespace TestApp;

            using System.Threading;
            using System.Threading.Tasks;
            using Interlocutor.Abstractions.Queries;
            using Interlocutor.CommonKernel;

            public interface IEntity;

            public sealed class Foo : IEntity
            {
                public Foo() { }
            }

            public sealed class GenericQuery<T> : IQuery<Outcome<T, string>>;

            public sealed class GenericQueryHandlerOne<T> : IQueryHandler<GenericQuery<T>, Outcome<T, string>>
                where T : IEntity, new()
            {
                public Task<Outcome<T, string>> HandleAsync(GenericQuery<T> query, CancellationToken ct) =>
                    throw new System.NotImplementedException();
            }

            public sealed class GenericQueryHandlerTwo<T> : IQueryHandler<GenericQuery<T>, Outcome<T, string>>
                where T : IEntity, new()
            {
                public Task<Outcome<T, string>> HandleAsync(GenericQuery<T> query, CancellationToken ct) =>
                    throw new System.NotImplementedException();
            }

            public sealed class UsesFoo
            {
                public void Use()
                {
                    var query = new GenericQuery<Foo>();
                }
            }
            """;

        // Act
        var result = GeneratorTestHarness.Run(source);

        // Assert
        Assert.Equal(2, result.Diagnostics.Length);
        Assert.All(result.Diagnostics, d => Assert.Equal("INTERLOC006", d.Id));
        Assert.All(
            result.Diagnostics,
            d => Assert.Contains("GenericQueryHandlerOne", d.GetMessage(), StringComparison.Ordinal)
        );
        Assert.All(
            result.Diagnostics,
            d => Assert.Contains("GenericQueryHandlerTwo", d.GetMessage(), StringComparison.Ordinal)
        );

        var registrationsSource = GetGeneratedSource(result, "InterlocutorGeneratedRegistrations.g.cs");
        Assert.DoesNotContain("GenericQueryHandlerOne<global::TestApp.Foo>", registrationsSource, StringComparison.Ordinal);
        Assert.DoesNotContain("GenericQueryHandlerTwo<global::TestApp.Foo>", registrationsSource, StringComparison.Ordinal);
    }

    [Fact]
    public void AmbiguousUsage_ReportsOnlyOpenGenericHandlerAmbiguity_NotAlsoUnresolvedGenericRequestUsage_Test()
    {
        // Regression for ticket 15: a usage at least one local open generic handler's request definition key
        // matched - even when that match is left ambiguous rather than resolved - must not additionally be
        // reported as INTERLOC008 (UnresolvedGenericRequestUsage). Reusing the exact ambiguity fixture above:
        // GenericQuery<Foo> is matched by two open handlers, so it is "attempted" (see
        // OpenGenericHandlerResolver.Result.AttemptedUsageDisplayNames), just never resolved.
        // Arrange
        const string source = """
            namespace TestApp;

            using System.Threading;
            using System.Threading.Tasks;
            using Interlocutor.Abstractions.Queries;
            using Interlocutor.CommonKernel;

            public interface IEntity;

            public sealed class Foo : IEntity
            {
                public Foo() { }
            }

            public sealed class GenericQuery<T> : IQuery<Outcome<T, string>>;

            public sealed class GenericQueryHandlerOne<T> : IQueryHandler<GenericQuery<T>, Outcome<T, string>>
                where T : IEntity, new()
            {
                public Task<Outcome<T, string>> HandleAsync(GenericQuery<T> query, CancellationToken ct) =>
                    throw new System.NotImplementedException();
            }

            public sealed class GenericQueryHandlerTwo<T> : IQueryHandler<GenericQuery<T>, Outcome<T, string>>
                where T : IEntity, new()
            {
                public Task<Outcome<T, string>> HandleAsync(GenericQuery<T> query, CancellationToken ct) =>
                    throw new System.NotImplementedException();
            }

            public sealed class UsesFoo
            {
                public void Use()
                {
                    var query = new GenericQuery<Foo>();
                }
            }
            """;

        // Act
        var result = GeneratorTestHarness.Run(source);

        // Assert
        // Exactly the same two INTERLOC006 diagnostics as the ambiguity test above, and nothing else - in
        // particular, no INTERLOC008 for the same usage.
        Assert.Equal(2, result.Diagnostics.Length);
        Assert.All(result.Diagnostics, d => Assert.Equal("INTERLOC006", d.Id));
        Assert.DoesNotContain(result.Diagnostics, d => d.Id == "INTERLOC008");
    }

    [Fact]
    public void HandlerOverride_AmbiguityResolution_Resolves_Test()
    {
        // Arrange
        const string source = """
            namespace TestApp;

            using System.Threading;
            using System.Threading.Tasks;
            using Interlocutor.Abstractions.Core;
            using Interlocutor.Abstractions.Queries;
            using Interlocutor.CommonKernel;

            public interface IEntity;

            public sealed class Foo : IEntity
            {
                public Foo() { }
            }

            public sealed class GenericQuery<T> : IQuery<Outcome<T, string>>;

            public sealed class GenericQueryHandlerLosing<T> : IQueryHandler<GenericQuery<T>, Outcome<T, string>>
                where T : IEntity, new()
            {
                public Task<Outcome<T, string>> HandleAsync(GenericQuery<T> query, CancellationToken ct) =>
                    throw new System.NotImplementedException();
            }

            [HandlerOverride(typeof(GenericQueryHandlerLosing<>))]
            public sealed class GenericQueryHandlerWinning<T> : IQueryHandler<GenericQuery<T>, Outcome<T, string>>
                where T : IEntity, new()
            {
                public Task<Outcome<T, string>> HandleAsync(GenericQuery<T> query, CancellationToken ct) =>
                    throw new System.NotImplementedException();
            }

            public sealed class UsesFoo
            {
                public void Use()
                {
                    var query = new GenericQuery<Foo>();
                }
            }
            """;

        // Act
        var result = GeneratorTestHarness.Run(source);

        // Assert
        Assert.Empty(result.Diagnostics);

        var registrationsSource = GetGeneratedSource(result, "InterlocutorGeneratedRegistrations.g.cs");
        Assert.Contains(
            "global::TestApp.GenericQueryHandlerWinning<global::TestApp.Foo>",
            registrationsSource,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain("GenericQueryHandlerLosing<global::TestApp.Foo>", registrationsSource, StringComparison.Ordinal);
    }

    [Fact]
    public void HandlerOverrideValidation_MissingTarget_ReportsError_Test()
    {
        // Arrange
        const string source = """
            namespace TestApp;

            using System.Threading;
            using System.Threading.Tasks;
            using Interlocutor.Abstractions.Core;
            using Interlocutor.Abstractions.Queries;
            using Interlocutor.CommonKernel;

            public interface IEntity;

            public sealed class GenericQuery<T> : IQuery<Outcome<T, string>>;

            public sealed class DoesNotExistElsewhere;

            [HandlerOverride(typeof(DoesNotExistElsewhere))]
            public sealed class GenericQueryHandler<T> : IQueryHandler<GenericQuery<T>, Outcome<T, string>>
                where T : IEntity, new()
            {
                public Task<Outcome<T, string>> HandleAsync(GenericQuery<T> query, CancellationToken ct) =>
                    throw new System.NotImplementedException();
            }
            """;

        // Act
        var result = GeneratorTestHarness.Run(source);

        // Assert
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("INTERLOC007", diagnostic.Id);
        Assert.Contains("GenericQueryHandler", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("DoesNotExistElsewhere", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void HandlerOverrideValidation_InvalidType_ReportsError_Test()
    {
        // Arrange
        const string source = """
            namespace TestApp;

            using System.Threading;
            using System.Threading.Tasks;
            using Interlocutor.Abstractions.Core;
            using Interlocutor.Abstractions.Queries;
            using Interlocutor.CommonKernel;

            public interface IEntity;

            public sealed class GenericQuery<T> : IQuery<Outcome<T, string>>;

            // A real type in the compilation, but not a handler at all.
            public sealed class NotAHandler;

            [HandlerOverride(typeof(NotAHandler))]
            public sealed class GenericQueryHandler<T> : IQueryHandler<GenericQuery<T>, Outcome<T, string>>
                where T : IEntity, new()
            {
                public Task<Outcome<T, string>> HandleAsync(GenericQuery<T> query, CancellationToken ct) =>
                    throw new System.NotImplementedException();
            }
            """;

        // Act
        var result = GeneratorTestHarness.Run(source);

        // Assert
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("INTERLOC007", diagnostic.Id);
        Assert.Contains("NotAHandler", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void NamespaceIsolation_OpenGenericHandler_DispatchUnchanged_Test()
    {
        // Arrange
        const string source = """
            namespace TestApp.SomeDeeplyNested.Namespace;

            using System.Threading;
            using System.Threading.Tasks;
            using Interlocutor.Abstractions.Queries;
            using Interlocutor.CommonKernel;

            public interface IEntity;

            public sealed class Foo : IEntity
            {
                public Foo() { }
            }

            public sealed class GenericQuery<T> : IQuery<Outcome<T, string>>;

            public sealed class GenericQueryHandler<T> : IQueryHandler<GenericQuery<T>, Outcome<T, string>>
                where T : IEntity, new()
            {
                public Task<Outcome<T, string>> HandleAsync(GenericQuery<T> query, CancellationToken ct) =>
                    throw new System.NotImplementedException();
            }

            public sealed class UsesFoo
            {
                public void Use()
                {
                    var query = new GenericQuery<Foo>();
                }
            }
            """;

        // Act
        var result = GeneratorTestHarness.Run(source);

        // Assert
        Assert.Empty(result.Diagnostics);

        var registrationsSource = GetGeneratedSource(result, "InterlocutorGeneratedRegistrations.g.cs");
        Assert.Contains(
            "global::TestApp.SomeDeeplyNested.Namespace.GenericQueryHandler<global::TestApp.SomeDeeplyNested.Namespace.Foo>",
            registrationsSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void ClosedGenericHandler_ConstructorDependency_ResolvesFromContainer_Test()
    {
        // Arrange
        const string source = """
            namespace TestApp;

            using System.Threading;
            using System.Threading.Tasks;
            using Interlocutor.Abstractions.Queries;
            using Interlocutor.CommonKernel;

            public interface IEntity;

            public interface ISomeDependency;

            public sealed class Foo : IEntity
            {
                public Foo() { }
            }

            public sealed class GenericQuery<T> : IQuery<Outcome<T, string>>;

            public sealed class GenericQueryHandler<T>(ISomeDependency dependency) : IQueryHandler<GenericQuery<T>, Outcome<T, string>>
                where T : IEntity, new()
            {
                public Task<Outcome<T, string>> HandleAsync(GenericQuery<T> query, CancellationToken ct) =>
                    throw new System.NotImplementedException();
            }

            public sealed class UsesFoo
            {
                public void Use()
                {
                    var query = new GenericQuery<Foo>();
                }
            }
            """;

        // Act
        var outputCompilation = GeneratorTestHarness.RunAndUpdate(source, out var generatorDiagnostics);

        // Assert
        Assert.Empty(generatorDiagnostics);

        var errors = outputCompilation
            .GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();
        Assert.Empty(errors);

        var dispatchSource = outputCompilation
            .SyntaxTrees.First(t => t.FilePath.EndsWith("InterlocutorGeneratedDispatch.g.cs", StringComparison.Ordinal))
            .ToString();

        // The closed handler is resolved by its closed service interface type, exactly like any other closed
        // handler - never via reflection over its constructor.
        Assert.Contains(
            "services.GetService(typeof(global::Interlocutor.Abstractions.Queries.IQueryHandler<global::TestApp.GenericQuery<global::TestApp.Foo>, global::Interlocutor.CommonKernel.Outcome<global::TestApp.Foo, string>>))",
            dispatchSource,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain("Activator.CreateInstance", dispatchSource, StringComparison.Ordinal);
    }

    private static string GetGeneratedSource(GeneratorDriverRunResult result, string hintNameSuffix) =>
        result
            .Results.Single()
            .GeneratedSources.Single(s => s.HintName.EndsWith(hintNameSuffix, StringComparison.Ordinal))
            .SourceText.ToString();
}
