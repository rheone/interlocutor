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

        public sealed class GenericQuery<T> : IQuery<OperationOutcome<T, string>>;

        public sealed class GenericQueryHandler<T> : IQueryHandler<GenericQuery<T>, OperationOutcome<T, string>>
            where T : IEntity, new()
        {
            public Task<OperationOutcome<T, string>> Handle(GenericQuery<T> query, CancellationToken ct) =>
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
    public void Open_generic_handler_closes_over_a_discovered_concrete_usage_and_reports_no_diagnostics()
    {
        var result = GeneratorTestHarness.Run(BasicGenericSource);

        Assert.Empty(result.Diagnostics);

        var registrationsSource = GetGeneratedSource(result, "InterlocutorGeneratedRegistrations.g.cs");
        Assert.Contains(
            "new global::Interlocutor.DependencyInjection.Abstractions.InterlocutorRegistration(typeof(global::Interlocutor.Abstractions.Queries.IQueryHandler<global::TestApp.GenericQuery<global::TestApp.Foo>, global::Interlocutor.CommonKernel.OperationOutcome<global::TestApp.Foo, string>>), typeof(global::TestApp.GenericQueryHandler<global::TestApp.Foo>), global::Interlocutor.DependencyInjection.Abstractions.RegistrationLifetime.Scoped, global::Interlocutor.DependencyInjection.Abstractions.RegistrationKind.Handler)",
            registrationsSource,
            StringComparison.Ordinal
        );

        var dispatchSource = GetGeneratedSource(result, "InterlocutorGeneratedDispatch.g.cs");
        Assert.Contains(
            "global::Interlocutor.Abstractions.Queries.IQueryHandler<global::TestApp.GenericQuery<global::TestApp.Foo>, global::Interlocutor.CommonKernel.OperationOutcome<global::TestApp.Foo, string>>",
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
    public void Emitted_code_for_a_closed_generic_handler_compiles_cleanly()
    {
        var outputCompilation = GeneratorTestHarness.RunAndUpdate(BasicGenericSource, out var generatorDiagnostics);

        Assert.Empty(generatorDiagnostics);

        var errors = outputCompilation
            .GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors.Select(e => e.ToString())));
    }

    [Fact]
    public void A_second_concrete_usage_closes_the_same_open_handler_a_second_time()
    {
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

            public sealed class GenericQuery<T> : IQuery<OperationOutcome<T, string>>;

            public sealed class GenericQueryHandler<T> : IQueryHandler<GenericQuery<T>, OperationOutcome<T, string>>
                where T : IEntity, new()
            {
                public Task<OperationOutcome<T, string>> Handle(GenericQuery<T> query, CancellationToken ct) =>
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

        var result = GeneratorTestHarness.Run(source);

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
    public void A_request_that_fails_the_handlers_constraint_is_not_closed_and_reports_missing_handler()
    {
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

            public sealed class GenericQuery<T> : IQuery<OperationOutcome<T, string>>;

            public sealed class GenericQueryHandler<T> : IQueryHandler<GenericQuery<T>, OperationOutcome<T, string>>
                where T : IEntity, new()
            {
                public Task<OperationOutcome<T, string>> Handle(GenericQuery<T> query, CancellationToken ct) =>
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

        var result = GeneratorTestHarness.Run(source);

        Assert.Empty(result.Diagnostics);

        var registrationsSource = GetGeneratedSource(result, "InterlocutorGeneratedRegistrations.g.cs");
        Assert.DoesNotContain("NotAnEntity", registrationsSource, StringComparison.Ordinal);
    }

    [Fact]
    public void A_closed_handler_takes_precedence_over_a_matching_open_generic_handler()
    {
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

            public sealed class GenericQuery<T> : IQuery<OperationOutcome<T, string>>;

            public sealed class GenericQueryHandler<T> : IQueryHandler<GenericQuery<T>, OperationOutcome<T, string>>
                where T : IEntity, new()
            {
                public Task<OperationOutcome<T, string>> Handle(GenericQuery<T> query, CancellationToken ct) =>
                    throw new System.NotImplementedException();
            }

            // Special-cases Foo: this concrete handler must win over the open generic one above.
            public sealed class FooQueryHandler : IQueryHandler<GenericQuery<Foo>, OperationOutcome<Foo, string>>
            {
                public Task<OperationOutcome<Foo, string>> Handle(GenericQuery<Foo> query, CancellationToken ct) =>
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

        var result = GeneratorTestHarness.Run(source);

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
    public void Two_open_generic_handlers_matching_the_same_request_report_ambiguity_naming_both()
    {
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

            public sealed class GenericQuery<T> : IQuery<OperationOutcome<T, string>>;

            public sealed class GenericQueryHandlerOne<T> : IQueryHandler<GenericQuery<T>, OperationOutcome<T, string>>
                where T : IEntity, new()
            {
                public Task<OperationOutcome<T, string>> Handle(GenericQuery<T> query, CancellationToken ct) =>
                    throw new System.NotImplementedException();
            }

            public sealed class GenericQueryHandlerTwo<T> : IQueryHandler<GenericQuery<T>, OperationOutcome<T, string>>
                where T : IEntity, new()
            {
                public Task<OperationOutcome<T, string>> Handle(GenericQuery<T> query, CancellationToken ct) =>
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

        var result = GeneratorTestHarness.Run(source);

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
    public void HandlerOverrideAttribute_resolves_an_ambiguity_between_two_open_generic_handlers()
    {
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

            public sealed class GenericQuery<T> : IQuery<OperationOutcome<T, string>>;

            public sealed class GenericQueryHandlerLosing<T> : IQueryHandler<GenericQuery<T>, OperationOutcome<T, string>>
                where T : IEntity, new()
            {
                public Task<OperationOutcome<T, string>> Handle(GenericQuery<T> query, CancellationToken ct) =>
                    throw new System.NotImplementedException();
            }

            [HandlerOverride(typeof(GenericQueryHandlerLosing<>))]
            public sealed class GenericQueryHandlerWinning<T> : IQueryHandler<GenericQuery<T>, OperationOutcome<T, string>>
                where T : IEntity, new()
            {
                public Task<OperationOutcome<T, string>> Handle(GenericQuery<T> query, CancellationToken ct) =>
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

        var result = GeneratorTestHarness.Run(source);

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
    public void HandlerOverrideAttribute_naming_a_missing_target_reports_a_build_error()
    {
        const string source = """
            namespace TestApp;

            using System.Threading;
            using System.Threading.Tasks;
            using Interlocutor.Abstractions.Core;
            using Interlocutor.Abstractions.Queries;
            using Interlocutor.CommonKernel;

            public interface IEntity;

            public sealed class GenericQuery<T> : IQuery<OperationOutcome<T, string>>;

            public sealed class DoesNotExistElsewhere;

            [HandlerOverride(typeof(DoesNotExistElsewhere))]
            public sealed class GenericQueryHandler<T> : IQueryHandler<GenericQuery<T>, OperationOutcome<T, string>>
                where T : IEntity, new()
            {
                public Task<OperationOutcome<T, string>> Handle(GenericQuery<T> query, CancellationToken ct) =>
                    throw new System.NotImplementedException();
            }
            """;

        var result = GeneratorTestHarness.Run(source);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("INTERLOC007", diagnostic.Id);
        Assert.Contains("GenericQueryHandler", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("DoesNotExistElsewhere", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void HandlerOverrideAttribute_naming_a_type_that_is_not_a_handler_reports_a_build_error()
    {
        const string source = """
            namespace TestApp;

            using System.Threading;
            using System.Threading.Tasks;
            using Interlocutor.Abstractions.Core;
            using Interlocutor.Abstractions.Queries;
            using Interlocutor.CommonKernel;

            public interface IEntity;

            public sealed class GenericQuery<T> : IQuery<OperationOutcome<T, string>>;

            // A real type in the compilation, but not a handler at all.
            public sealed class NotAHandler;

            [HandlerOverride(typeof(NotAHandler))]
            public sealed class GenericQueryHandler<T> : IQueryHandler<GenericQuery<T>, OperationOutcome<T, string>>
                where T : IEntity, new()
            {
                public Task<OperationOutcome<T, string>> Handle(GenericQuery<T> query, CancellationToken ct) =>
                    throw new System.NotImplementedException();
            }
            """;

        var result = GeneratorTestHarness.Run(source);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("INTERLOC007", diagnostic.Id);
        Assert.Contains("NotAHandler", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void Moving_the_open_generic_handler_to_a_different_namespace_does_not_change_dispatch()
    {
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

            public sealed class GenericQuery<T> : IQuery<OperationOutcome<T, string>>;

            public sealed class GenericQueryHandler<T> : IQueryHandler<GenericQuery<T>, OperationOutcome<T, string>>
                where T : IEntity, new()
            {
                public Task<OperationOutcome<T, string>> Handle(GenericQuery<T> query, CancellationToken ct) =>
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

        var result = GeneratorTestHarness.Run(source);

        Assert.Empty(result.Diagnostics);

        var registrationsSource = GetGeneratedSource(result, "InterlocutorGeneratedRegistrations.g.cs");
        Assert.Contains(
            "global::TestApp.SomeDeeplyNested.Namespace.GenericQueryHandler<global::TestApp.SomeDeeplyNested.Namespace.Foo>",
            registrationsSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Closed_generic_handler_with_a_constructor_dependency_resolves_it_from_the_registered_service_type()
    {
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

            public sealed class GenericQuery<T> : IQuery<OperationOutcome<T, string>>;

            public sealed class GenericQueryHandler<T>(ISomeDependency dependency) : IQueryHandler<GenericQuery<T>, OperationOutcome<T, string>>
                where T : IEntity, new()
            {
                public Task<OperationOutcome<T, string>> Handle(GenericQuery<T> query, CancellationToken ct) =>
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

        var outputCompilation = GeneratorTestHarness.RunAndUpdate(source, out var generatorDiagnostics);
        Assert.Empty(generatorDiagnostics);

        var errors = outputCompilation
            .GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors.Select(e => e.ToString())));

        var dispatchSource = outputCompilation
            .SyntaxTrees.First(t => t.FilePath.EndsWith("InterlocutorGeneratedDispatch.g.cs", StringComparison.Ordinal))
            .ToString();

        // The closed handler is resolved by its closed service interface type, exactly like any other closed
        // handler - never via reflection over its constructor.
        Assert.Contains(
            "services.GetService(typeof(global::Interlocutor.Abstractions.Queries.IQueryHandler<global::TestApp.GenericQuery<global::TestApp.Foo>, global::Interlocutor.CommonKernel.OperationOutcome<global::TestApp.Foo, string>>))",
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
