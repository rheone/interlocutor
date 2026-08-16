namespace Interlocutor.Architecture.Tests;

using System.Reflection;
using Interlocutor.Abstractions.Core;
using Interlocutor.CommonKernel;
using NetArchTest.Rules;

public class DependencyTests
{
    private static readonly Assembly CommonKernelAssembly = typeof(OperationOutcome).Assembly;
    private static readonly Assembly AbstractionsAssembly = typeof(IMediator).Assembly;
    private static readonly Assembly DependencyInjectionAbstractionsAssembly =
        typeof(Interlocutor.DependencyInjection.Abstractions.InterlocutorRegistration).Assembly;

    [Fact]
    public void CommonKernel_ShouldNotDependOnOtherInterlocutorPackages()
    {
        var result = Types
            .InAssembly(CommonKernelAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Interlocutor.Abstractions",
                "Interlocutor.AspNetCore",
                "Interlocutor.EntityFrameworkCore",
                "Interlocutor.FluentValidation",
                "Interlocutor.NHibernate"
            )
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Abstractions_ShouldNotDependOnAspnetcoreOrExamplePackages()
    {
        var result = Types
            .InAssembly(AbstractionsAssembly)
            .ShouldNot()
            .HaveDependencyOnAny([
                "Interlocutor.AspNetCore",
                "Interlocutor.EntityFrameworkCore",
                "Interlocutor.FluentValidation",
                "Interlocutor.NHibernate",
            ])
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void AspNetCore_ShouldNotDependOnStoreOrValidationPackages()
    {
        var result = Types
            .InAssembly(typeof(Interlocutor.AspNetCore.ResultExtensions).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny([
                "Interlocutor.EntityFrameworkCore",
                "Interlocutor.FluentValidation",
                "Interlocutor.NHibernate",
            ])
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void DependencyInjectionAbstractions_ShouldDependOnlyOnAbstractions()
    {
        var result = Types
            .InAssembly(DependencyInjectionAbstractionsAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Interlocutor.AspNetCore",
                "Interlocutor.DependencyInjection.Microsoft",
                "Interlocutor.DependencyInjection.Autofac",
                "Interlocutor.EntityFrameworkCore",
                "Interlocutor.FluentValidation",
                "Interlocutor.NHibernate"
            )
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));

        // Assembly-level (rather than per-type) check: the module owns the neutral registration
        // record alongside plain data types, so not every type references Abstractions directly -
        // only that the module as a whole depends on it, per the mediator abstractions module.
        var referencedAssemblyNames = DependencyInjectionAbstractionsAssembly.GetReferencedAssemblies().Select(a => a.Name);
        Assert.Contains(AbstractionsAssembly.GetName().Name, referencedAssemblyNames);
    }

    [Fact]
    public void ExampleImplementations_ShouldDependOnAbstractions()
    {
        var efCoreResult = Types
            .InAssembly(typeof(Interlocutor.EntityFrameworkCore.TransactionBehavior<,,>).Assembly)
            .Should()
            .HaveDependencyOn("Interlocutor.Abstractions")
            .GetResult();

        var fluentResult = Types
            .InAssembly(typeof(Interlocutor.FluentValidation.ValidationBehavior<,>).Assembly)
            .Should()
            .HaveDependencyOn("Interlocutor.Abstractions")
            .GetResult();

        var nhibernateResult = Types
            .InAssembly(typeof(Interlocutor.NHibernate.TransactionBehavior<,>).Assembly)
            .Should()
            .HaveDependencyOn("Interlocutor.Abstractions")
            .GetResult();

        Assert.True(efCoreResult.IsSuccessful, "EF Core package should depend on Abstractions");
        Assert.True(fluentResult.IsSuccessful, "FluentValidation package should depend on Abstractions");
        Assert.True(nhibernateResult.IsSuccessful, "NHibernate package should depend on Abstractions");
    }
}
