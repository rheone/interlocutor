namespace Mediance.Architecture.Tests;

using System.Reflection;
using Mediance.Abstractions.Core;
using Mediance.CommonKernel;
using NetArchTest.Rules;

public class DependencyTests
{
    private static readonly Assembly CommonKernelAssembly = typeof(Result).Assembly;
    private static readonly Assembly AbstractionsAssembly = typeof(IMediator).Assembly;

    [Fact]
    public void CommonKernel_ShouldNotDependOnOtherMediancePackages()
    {
        var result = Types
            .InAssembly(CommonKernelAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Mediance.Abstractions",
                "Mediance.AspNetCore",
                "Mediance.EntityFrameworkCore",
                "Mediance.FluentValidation",
                "Mediance.NHibernate"
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
                "Mediance.AspNetCore",
                "Mediance.EntityFrameworkCore",
                "Mediance.FluentValidation",
                "Mediance.NHibernate",
            ])
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void AspNetCore_ShouldNotDependOnAbstractionsOrBehaviors()
    {
        var result = Types
            .InAssembly(typeof(Mediance.AspNetCore.ResultExtensions).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny([
                "Mediance.Abstractions",
                "Mediance.EntityFrameworkCore",
                "Mediance.FluentValidation",
                "Mediance.NHibernate",
            ])
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void ExampleImplementations_ShouldDependOnAbstractions()
    {
        var efCoreResult = Types
            .InAssembly(typeof(Mediance.EntityFrameworkCore.TransactionBehavior<,,>).Assembly)
            .Should()
            .HaveDependencyOn("Mediance.Abstractions")
            .GetResult();

        // ValidationBehavior has been commented out during code migration
        // var fluentResult = Types
        //     .InAssembly(typeof(Mediance.FluentValidation.ValidationBehavior<,>).Assembly)
        //     .Should()
        //     .HaveDependencyOn("Mediance.Abstractions")
        //     .GetResult();

        var nhibernateResult = Types
            .InAssembly(typeof(Mediance.NHibernate.TransactionBehavior<,>).Assembly)
            .Should()
            .HaveDependencyOn("Mediance.Abstractions")
            .GetResult();

        Assert.True(efCoreResult.IsSuccessful, "EF Core package should depend on Abstractions");
        // Assert.True(fluentResult.IsSuccessful, "FluentValidation package should depend on Abstractions");
        Assert.True(nhibernateResult.IsSuccessful, "NHibernate package should depend on Abstractions");
    }
}
