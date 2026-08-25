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
    public void CommonKernel_ReferencedAssembly_HasNoDependencyOnOtherInterlocutorPackages_Test()
    {
        // Arrange
        // (static class-level assembly references serve as fixture)

        // Act
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

        // Assert
        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Abstractions_ReferencedAssembly_HasNoDependencyOnAspnetcoreOrExamplePackages_Test()
    {
        // Arrange
        // (static class-level assembly references serve as fixture)

        // Act
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

        // Assert
        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void AspNetCore_ReferencedAssembly_HasNoDependencyOnStoreOrValidationPackages_Test()
    {
        // Arrange
        // (static class-level assembly references serve as fixture)

        // Act
        var result = Types
            .InAssembly(typeof(Interlocutor.AspNetCore.ResultExtensions).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny([
                "Interlocutor.EntityFrameworkCore",
                "Interlocutor.FluentValidation",
                "Interlocutor.NHibernate",
            ])
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void DependencyInjectionAbstractions_ReferencedAssembly_DependsOnlyOnAbstractions_Test()
    {
        // Arrange
        // (static class-level assembly references serve as fixture)

        // Act
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

        // Assert
        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));

        // Act (second check) - assembly-level (rather than per-type): the module owns the neutral
        // registration record alongside plain data types, so not every type references Abstractions
        // directly - only that the module as a whole depends on it, per the mediator abstractions module.
        var referencedAssemblyNames = DependencyInjectionAbstractionsAssembly.GetReferencedAssemblies().Select(a => a.Name);

        // Assert (second check)
        Assert.Contains(AbstractionsAssembly.GetName().Name, referencedAssemblyNames);
    }

    [Fact]
    public void ExampleImplementations_ReferencedAssembly_DependsOnAbstractions_Test()
    {
        // Arrange
        // (static class-level assembly references serve as fixture)

        // Act
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

        // Assert
        Assert.True(efCoreResult.IsSuccessful, "EF Core package should depend on Abstractions");
        Assert.True(fluentResult.IsSuccessful, "FluentValidation package should depend on Abstractions");
        Assert.True(nhibernateResult.IsSuccessful, "NHibernate package should depend on Abstractions");
    }

    [Fact]
    public void DependencyInjectionMicrosoft_ReferencedAssembly_HasNoDependencyOnAutofacOrStoreOrValidationPackages_Test()
    {
        // Arrange
        // (static class-level assembly references serve as fixture)

        // Act
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

        // Assert
        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void DependencyInjectionAutofac_ReferencedAssembly_HasNoDependencyOnMicrosoftOrStoreOrValidationPackages_Test()
    {
        // Arrange
        // (static class-level assembly references serve as fixture)

        // Act
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

        // Assert
        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void EntityFrameworkCore_ReferencedAssembly_HasNoDependencyOnNHibernateOrFluentValidation_Test()
    {
        // Arrange
        // (static class-level assembly references serve as fixture)

        // Act
        var result = Types
            .InAssembly(EntityFrameworkCoreAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(["Interlocutor.NHibernate", "Interlocutor.FluentValidation"])
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void NHibernate_ReferencedAssembly_HasNoDependencyOnEntityFrameworkCoreOrFluentValidation_Test()
    {
        // Arrange
        // (static class-level assembly references serve as fixture)

        // Act
        var result = Types
            .InAssembly(NHibernateAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(["Interlocutor.EntityFrameworkCore", "Interlocutor.FluentValidation"])
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void PersistenceAbstractions_ReferencedAssembly_HasNoDependencyOnAspnetcoreStoreValidationOrDependencyInjectionPackages_Test()
    {
        // Arrange
        // (static class-level assembly references serve as fixture)

        // Act
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

        // Assert
        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void PersistenceEntityFrameworkCore_ShouldDependOnPersistenceAbstractions_AndNotOnPersistenceNHibernate_Test()
    {
        // Arrange
        // (static class-level assembly references serve as fixture)

        // Act
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

        // Assert
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
    public void PersistenceNHibernate_ShouldDependOnPersistenceAbstractions_AndNotOnPersistenceEntityFrameworkCore_Test()
    {
        // Arrange
        // (static class-level assembly references serve as fixture)

        // Act
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

        // Assert
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
