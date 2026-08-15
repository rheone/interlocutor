namespace Interlocutor.Architecture.Tests;

using System.Reflection;
using Interlocutor.Abstractions.Core;
using Interlocutor.CommonKernel;
using NetArchTest.Rules;

public class DependencyTests
{
    private static readonly Assembly CommonKernelAssembly = typeof(OperationOutcome).Assembly;
    private static readonly Assembly AbstractionsAssembly = typeof(IMediator).Assembly;

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
