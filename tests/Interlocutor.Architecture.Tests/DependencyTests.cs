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
    private static readonly Assembly DependencyInjectionMicrosoftAssembly =
        typeof(Interlocutor.DependencyInjection.Microsoft.InterlocutorServiceCollectionExtensions).Assembly;
    private static readonly Assembly DependencyInjectionAutofacAssembly =
        typeof(Interlocutor.DependencyInjection.Autofac.InterlocutorAutofacExtensions).Assembly;
    private static readonly Assembly EntityFrameworkCoreAssembly =
        typeof(Interlocutor.EntityFrameworkCore.TransactionBehavior<,,>).Assembly;
    private static readonly Assembly NHibernateAssembly = typeof(Interlocutor.NHibernate.TransactionBehavior<,>).Assembly;
    private static readonly Assembly FluentValidationAssembly =
        typeof(Interlocutor.FluentValidation.ValidationBehavior<,>).Assembly;
    private static readonly Assembly PersistenceAbstractionsAssembly =
        typeof(Interlocutor.Persistence.Abstractions.IUnitOfWork).Assembly;
    private static readonly Assembly PersistenceEntityFrameworkCoreAssembly =
        typeof(Interlocutor.Persistence.EntityFrameworkCore.EntityFrameworkUnitOfWork<>).Assembly;
    private static readonly Assembly PersistenceNHibernateAssembly =
        typeof(Interlocutor.Persistence.NHibernate.NHibernateUnitOfWork).Assembly;

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
            .InAssembly(EntityFrameworkCoreAssembly)
            .Should()
            .HaveDependencyOn("Interlocutor.Abstractions")
            .GetResult();

        var fluentResult = Types
            .InAssembly(FluentValidationAssembly)
            .Should()
            .HaveDependencyOn("Interlocutor.Abstractions")
            .GetResult();

        var nhibernateResult = Types
            .InAssembly(NHibernateAssembly)
            .Should()
            .HaveDependencyOn("Interlocutor.Abstractions")
            .GetResult();

        Assert.True(efCoreResult.IsSuccessful, "EF Core package should depend on Abstractions");
        Assert.True(fluentResult.IsSuccessful, "FluentValidation package should depend on Abstractions");
        Assert.True(nhibernateResult.IsSuccessful, "NHibernate package should depend on Abstractions");
    }

    [Fact]
    public void DependencyInjectionMicrosoft_ShouldNotDependOnAutofacOrStoreOrValidationPackages()
    {
        var result = Types
            .InAssembly(DependencyInjectionMicrosoftAssembly)
            .ShouldNot()
            .HaveDependencyOnAny([
                "Interlocutor.DependencyInjection.Autofac",
                "Interlocutor.EntityFrameworkCore",
                "Interlocutor.FluentValidation",
                "Interlocutor.NHibernate",
            ])
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void DependencyInjectionAutofac_ShouldNotDependOnMicrosoftOrStoreOrValidationPackages()
    {
        var result = Types
            .InAssembly(DependencyInjectionAutofacAssembly)
            .ShouldNot()
            .HaveDependencyOnAny([
                "Interlocutor.DependencyInjection.Microsoft",
                "Interlocutor.EntityFrameworkCore",
                "Interlocutor.FluentValidation",
                "Interlocutor.NHibernate",
            ])
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void EntityFrameworkCore_ShouldNotDependOnNHibernateOrFluentValidation()
    {
        var result = Types
            .InAssembly(EntityFrameworkCoreAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(["Interlocutor.NHibernate", "Interlocutor.FluentValidation"])
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void NHibernate_ShouldNotDependOnEntityFrameworkCoreOrFluentValidation()
    {
        var result = Types
            .InAssembly(NHibernateAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(["Interlocutor.EntityFrameworkCore", "Interlocutor.FluentValidation"])
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void PersistenceAbstractions_ShouldNotDependOnAspnetcoreStoreValidationOrDependencyInjectionPackages()
    {
        var result = Types
            .InAssembly(PersistenceAbstractionsAssembly)
            .ShouldNot()
            .HaveDependencyOnAny([
                "Interlocutor.AspNetCore",
                "Interlocutor.EntityFrameworkCore",
                "Interlocutor.Persistence.EntityFrameworkCore",
                "Interlocutor.FluentValidation",
                "Interlocutor.NHibernate",
                "Interlocutor.Persistence.NHibernate",
                "Interlocutor.DependencyInjection.Microsoft",
                "Interlocutor.DependencyInjection.Autofac",
            ])
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void PersistenceEntityFrameworkCore_ShouldDependOnPersistenceAbstractions_AndNotOnPersistenceNHibernate()
    {
        var dependsOnAbstractionsResult = Types
            .InAssembly(PersistenceEntityFrameworkCoreAssembly)
            .Should()
            .HaveDependencyOn("Interlocutor.Persistence.Abstractions")
            .GetResult();

        var doesNotDependOnNHibernateResult = Types
            .InAssembly(PersistenceEntityFrameworkCoreAssembly)
            .ShouldNot()
            .HaveDependencyOn("Interlocutor.Persistence.NHibernate")
            .GetResult();

        Assert.True(
            dependsOnAbstractionsResult.IsSuccessful,
            "Persistence.EntityFrameworkCore should depend on Persistence.Abstractions"
        );
        Assert.True(
            doesNotDependOnNHibernateResult.IsSuccessful,
            string.Join(", ", doesNotDependOnNHibernateResult.FailingTypeNames ?? [])
        );
    }

    [Fact]
    public void PersistenceNHibernate_ShouldDependOnPersistenceAbstractions_AndNotOnPersistenceEntityFrameworkCore()
    {
        var dependsOnAbstractionsResult = Types
            .InAssembly(PersistenceNHibernateAssembly)
            .Should()
            .HaveDependencyOn("Interlocutor.Persistence.Abstractions")
            .GetResult();

        var doesNotDependOnEntityFrameworkCoreResult = Types
            .InAssembly(PersistenceNHibernateAssembly)
            .ShouldNot()
            .HaveDependencyOn("Interlocutor.Persistence.EntityFrameworkCore")
            .GetResult();

        Assert.True(
            dependsOnAbstractionsResult.IsSuccessful,
            "Persistence.NHibernate should depend on Persistence.Abstractions"
        );
        Assert.True(
            doesNotDependOnEntityFrameworkCoreResult.IsSuccessful,
            string.Join(", ", doesNotDependOnEntityFrameworkCoreResult.FailingTypeNames ?? [])
        );
    }
}
