namespace Interlocutor.Architecture.Tests;

using System.Reflection;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.CommonKernel;
using NetArchTest.Rules;

public class DependencyTests
{
    private static readonly Assembly CommonKernelAssembly = typeof(Outcome).Assembly;
    private static readonly Assembly AbstractionsAssembly = typeof(IMediator).Assembly;
    private static readonly Assembly DependencyInjectionAbstractionsAssembly =
        typeof(Interlocutor.DependencyInjection.Abstractions.InterlocutorRegistration).Assembly;
    private static readonly Assembly DependencyInjectionMicrosoftAssembly =
        typeof(Interlocutor.DependencyInjection.Microsoft.InterlocutorServiceCollectionExtensions).Assembly;
    private static readonly Assembly DependencyInjectionAutofacAssembly =
        typeof(Interlocutor.DependencyInjection.Autofac.InterlocutorAutofacExtensions).Assembly;
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
                "Interlocutor.Persistence.EntityFrameworkCore",
                "Interlocutor.FluentValidation",
                "Interlocutor.Persistence.NHibernate"
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
            .HaveDependencyOnAny(
                "Interlocutor.AspNetCore",
                "Interlocutor.Persistence.EntityFrameworkCore",
                "Interlocutor.FluentValidation",
                "Interlocutor.Persistence.NHibernate"
            )
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
            .HaveDependencyOnAny(
                "Interlocutor.Persistence.EntityFrameworkCore",
                "Interlocutor.FluentValidation",
                "Interlocutor.Persistence.NHibernate"
            )
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
                "Interlocutor.Persistence.EntityFrameworkCore",
                "Interlocutor.FluentValidation",
                "Interlocutor.Persistence.NHibernate"
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

        // Act - assembly-level (rather than per-type): the module owns plain data types (FluentValidationErrors)
        // alongside its pipeline behaviors, so not every type references Abstractions directly - only that
        // the module as a whole depends on it, per the same pattern as DependencyInjectionAbstractions above.
        var referencedAssemblyNames = FluentValidationAssembly.GetReferencedAssemblies().Select(a => a.Name);

        // Assert
        Assert.Contains(AbstractionsAssembly.GetName().Name, referencedAssemblyNames);
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
            .HaveDependencyOnAny(
                "Interlocutor.DependencyInjection.Autofac",
                "Interlocutor.Persistence.EntityFrameworkCore",
                "Interlocutor.FluentValidation",
                "Interlocutor.Persistence.NHibernate"
            )
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
            .HaveDependencyOnAny(
                "Interlocutor.DependencyInjection.Microsoft",
                "Interlocutor.Persistence.EntityFrameworkCore",
                "Interlocutor.FluentValidation",
                "Interlocutor.Persistence.NHibernate"
            )
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
            .HaveDependencyOnAny(
                "Interlocutor.AspNetCore",
                "Interlocutor.Persistence.EntityFrameworkCore",
                "Interlocutor.FluentValidation",
                "Interlocutor.Persistence.NHibernate",
                "Interlocutor.DependencyInjection.Microsoft",
                "Interlocutor.DependencyInjection.Autofac"
            )
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
        // Assembly-level (rather than NetArchTest's per-type Should().HaveDependencyOn check): this assembly
        // also ships UnitOfWorkBehavior and UnitOfWorkSaveChangesInterceptor, neither of which individually
        // references Interlocutor.Persistence.Abstractions (they only need Interlocutor.Abstractions and EF
        // Core) - a per-type "every type must reference it" check fails on those two by design, even though
        // the assembly as a whole correctly depends on Persistence.Abstractions via EntityFrameworkUnitOfWork
        // and EntityFrameworkRepository. See DependencyInjectionAbstractions_ReferencedAssembly_DependsOnlyOnAbstractions_Test
        // for the same assembly-level idiom.
        var referencedAssemblyNames = PersistenceEntityFrameworkCoreAssembly.GetReferencedAssemblies().Select(a => a.Name);

        var doesNotDependOnNHibernateResult = Types
            .InAssembly(PersistenceEntityFrameworkCoreAssembly)
            .ShouldNot()
            .HaveDependencyOn("Interlocutor.Persistence.NHibernate")
            .GetResult();

        // Assert
        Assert.Contains(PersistenceAbstractionsAssembly.GetName().Name, referencedAssemblyNames);
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
        // Assembly-level, not per-type - see the EF Core counterpart's comment above for why: this assembly
        // also ships UnitOfWorkBehavior, which doesn't itself reference Interlocutor.Persistence.Abstractions.
        var referencedAssemblyNames = PersistenceNHibernateAssembly.GetReferencedAssemblies().Select(a => a.Name);

        var doesNotDependOnEntityFrameworkCoreResult = Types
            .InAssembly(PersistenceNHibernateAssembly)
            .ShouldNot()
            .HaveDependencyOn("Interlocutor.Persistence.EntityFrameworkCore")
            .GetResult();

        // Assert
        Assert.Contains(PersistenceAbstractionsAssembly.GetName().Name, referencedAssemblyNames);
        Assert.True(
            doesNotDependOnEntityFrameworkCoreResult.IsSuccessful,
            string.Join(", ", doesNotDependOnEntityFrameworkCoreResult.FailingTypeNames ?? [])
        );
    }

    [Fact]
    public void PersistenceEntityFrameworkCore_ReferencedAssembly_ContainsTheEfCoreCommitGateBehavior_Test()
    {
        // Per ADR-0018, the EF Core commit-gate behavior (superseding the retired Interlocutor.EntityFrameworkCore
        // package's TransactionBehavior) ships from this package, not a standalone one - this pins that placement.
        // Arrange
        // (static class-level assembly references serve as fixture)

        // Act
        var implementsPipelineBehavior = PersistenceEntityFrameworkCoreAssembly
            .GetTypes()
            .Any(t =>
                t.Name.StartsWith("UnitOfWorkBehavior", StringComparison.Ordinal)
                && t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
            );

        // Assert
        Assert.True(
            implementsPipelineBehavior,
            "Interlocutor.Persistence.EntityFrameworkCore should contain an IPipelineBehavior<,> named UnitOfWorkBehavior"
        );
    }

    [Fact]
    public void PersistenceNHibernate_ReferencedAssembly_ContainsTheNHibernateCommitGateBehavior_Test()
    {
        // Per ADR-0018, the NHibernate commit-gate behavior (superseding the retired Interlocutor.NHibernate
        // package's TransactionBehavior) ships from this package, not a standalone one - this pins that placement.
        // Arrange
        // (static class-level assembly references serve as fixture)

        // Act
        var implementsPipelineBehavior = PersistenceNHibernateAssembly
            .GetTypes()
            .Any(t =>
                t.Name.StartsWith("UnitOfWorkBehavior", StringComparison.Ordinal)
                && t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
            );

        // Assert
        Assert.True(
            implementsPipelineBehavior,
            "Interlocutor.Persistence.NHibernate should contain an IPipelineBehavior<,> named UnitOfWorkBehavior"
        );
    }
}
