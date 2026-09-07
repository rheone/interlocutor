namespace Interlocutor.DependencyInjection.Abstractions.Tests;

using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Notifications;
using Interlocutor.CommonKernel;

/// <summary>
/// Covers <see cref="RegistrationProblemKind.AmbiguousClosure"/> detection, <see cref="InterlocutorBuilder.Eject(Type[])"/>'s
/// conflict-clearing escape hatch, and <see cref="InterlocutorBuilder.RequireUniqueClosure"/>'s consumer-contract
/// opt-in - split off from <see cref="RegistrationValidatorTests"/> the same way that file itself was split off
/// from <see cref="InterlocutorBuilderTests"/> under ADR-0015, since this is its own self-contained behavioral
/// matrix with its own fixtures. Exercised entirely through the public <see cref="InterlocutorBuilder"/>/<see cref="InterlocutorBuilder.DescribeRegistrations"/>
/// surface, consistent with <see cref="RegistrationValidatorTests"/>.
/// </summary>
public class AmbiguousClosureTests
{
    private interface IClosureTestExportStrategy<T>
    {
        Task ExportAsync(T item, CancellationToken ct);
    }

    [Fact]
    public void ValidateOnBuild_DetectsAmbiguousClosure_WhenTwoImplementationsCloseTheSameCommandHandler()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder.ScanAssembly(typeof(AmbiguousClosureTests).Assembly).ValidateOnBuild();

        // Assert
        var problem = Assert.Single(
            builder.DescribeRegistrations().Problems,
            p => p.Kind == RegistrationProblemKind.AmbiguousClosure && p.Description.Contains(nameof(ConflictedCommand))
        );
        Assert.Contains(nameof(ConflictedCommandHandlerA), problem.Description);
        Assert.Contains(nameof(ConflictedCommandHandlerB), problem.Description);
    }

    [Fact]
    public void ValidateOnBuild_TwoNotificationHandlersForTheSameNotification_IsNeverAmbiguous()
    {
        // INotificationHandler<> fan-out is deliberately excluded from the AmbiguousClosure check - zero,
        // one, or many handlers per notification are all valid configurations.
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder.ScanAssembly(typeof(AmbiguousClosureTests).Assembly).ValidateOnBuild();

        // Assert
        Assert.DoesNotContain(
            builder.DescribeRegistrations().Problems,
            p => p.Kind == RegistrationProblemKind.AmbiguousClosure && p.Description.Contains(nameof(ClosureTestNotification))
        );
    }

    [Fact]
    public void ValidateOnBuild_SameImplementationDiscoveredTwice_IsNotAmbiguous()
    {
        // ConflictedCommandHandlerA is found once by scanning and once by AddHandler for the identical
        // (service type, implementation type) pair - InterlocutorBuilder.Handlers collapses that to one
        // registration, so with the other candidate ejected, zero conflicting implementations remain.
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder
            .ScanAssembly(typeof(AmbiguousClosureTests).Assembly)
            .AddHandler(typeof(ConflictedCommandHandlerA))
            .Eject(typeof(ConflictedCommandHandlerB))
            .ValidateOnBuild();

        // Assert
        Assert.DoesNotContain(
            builder.DescribeRegistrations().Problems,
            p => p.Kind == RegistrationProblemKind.AmbiguousClosure && p.Description.Contains(nameof(ConflictedCommand))
        );
    }

    [Fact]
    public void ValidateOnBuild_AddHandlerForOneConflictingCandidate_DoesNotClearAmbiguityForTheOther()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder
            .ScanAssembly(typeof(AmbiguousClosureTests).Assembly)
            .AddHandler(typeof(ConflictedCommandHandlerA))
            .ValidateOnBuild();

        // Assert
        var problem = Assert.Single(
            builder.DescribeRegistrations().Problems,
            p => p.Kind == RegistrationProblemKind.AmbiguousClosure && p.Description.Contains(nameof(ConflictedCommand))
        );
        Assert.Contains(nameof(ConflictedCommandHandlerA), problem.Description);
        Assert.Contains(nameof(ConflictedCommandHandlerB), problem.Description);
    }

    [Fact]
    public void Eject_RemovesImplementationFromHandlers()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder.ScanAssembly(typeof(AmbiguousClosureTests).Assembly).Eject(typeof(ConflictedCommandHandlerB));

        // Assert
        Assert.DoesNotContain(builder.Handlers, r => r.ImplementationType == typeof(ConflictedCommandHandlerB));
        Assert.Contains(builder.Handlers, r => r.ImplementationType == typeof(ConflictedCommandHandlerA));
    }

    [Fact]
    public void Eject_ClearsTheAmbiguousClosureProblem_LeavingTheOtherCandidateRegistered()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder
            .ScanAssembly(typeof(AmbiguousClosureTests).Assembly)
            .Eject(typeof(ConflictedCommandHandlerB))
            .ValidateOnBuild();

        // Assert
        Assert.DoesNotContain(
            builder.DescribeRegistrations().Problems,
            p => p.Kind == RegistrationProblemKind.AmbiguousClosure && p.Description.Contains(nameof(ConflictedCommand))
        );
        Assert.Contains(builder.Handlers, r => r.ImplementationType == typeof(ConflictedCommandHandlerA));
    }

    [Fact]
    public void Eject_Generic_BehavesIdenticallyToTheTypeOverload()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder.ScanAssembly(typeof(AmbiguousClosureTests).Assembly).Eject<ConflictedCommandHandlerB>();

        // Assert
        Assert.DoesNotContain(builder.Handlers, r => r.ImplementationType == typeof(ConflictedCommandHandlerB));
    }

    [Fact]
    public void Eject_CalledMoreThanOnce_AccumulatesRatherThanReplacing()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder.Eject(typeof(ConflictedCommandHandlerA)).Eject(typeof(ConflictedCommandHandlerB));

        // Assert
        Assert.Equal(
            [typeof(ConflictedCommandHandlerA), typeof(ConflictedCommandHandlerB)],
            builder.EjectedImplementationTypes
        );
    }

    [Fact]
    public void ValidateOnBuild_NotConfigured_LeavesAmbiguousClosureProblemsEmpty()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder.ScanAssembly(typeof(AmbiguousClosureTests).Assembly);

        // Assert
        Assert.Empty(builder.DescribeRegistrations().Problems);
    }

    [Fact]
    public void RequireUniqueClosure_RegistersEveryClosedImplementationFoundWhileScanning()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder
            .ScanAssembly(typeof(AmbiguousClosureTests).Assembly)
            .RequireUniqueClosure(typeof(IClosureTestExportStrategy<>));

        // Assert
        Assert.Contains(
            builder.Handlers,
            r =>
                r.ImplementationType == typeof(SoleExportStrategy)
                && r.ServiceType == typeof(IClosureTestExportStrategy<SoleExportItem>)
                && r.Kind == RegistrationKind.Handler
        );
    }

    [Fact]
    public void WithoutRequireUniqueClosure_ConsumerContractClosuresAreNeverRegistered()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder.ScanAssembly(typeof(AmbiguousClosureTests).Assembly);

        // Assert
        Assert.DoesNotContain(builder.Handlers, r => r.ImplementationType == typeof(SoleExportStrategy));
    }

    [Fact]
    public void ValidateOnBuild_RequireUniqueClosure_DetectsAmbiguousClosureForTheConsumerContract()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder
            .ScanAssembly(typeof(AmbiguousClosureTests).Assembly)
            .RequireUniqueClosure(typeof(IClosureTestExportStrategy<>))
            .ValidateOnBuild();

        // Assert
        var problem = Assert.Single(
            builder.DescribeRegistrations().Problems,
            p =>
                p.Kind == RegistrationProblemKind.AmbiguousClosure
                && p.Description.Contains(nameof(FirstConflictingExportStrategy))
        );
        Assert.Contains(nameof(FirstConflictingExportStrategy), problem.Description);
        Assert.Contains(nameof(SecondConflictingExportStrategy), problem.Description);
    }

    [Fact]
    public void ValidateOnBuild_WithoutRequireUniqueClosure_TheConsumerContractConflictIsNeverChecked()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder.ScanAssembly(typeof(AmbiguousClosureTests).Assembly).ValidateOnBuild();

        // Assert
        Assert.DoesNotContain(
            builder.DescribeRegistrations().Problems,
            p =>
                p.Kind == RegistrationProblemKind.AmbiguousClosure
                && p.Description.Contains(nameof(FirstConflictingExportStrategy))
        );
    }

    [Fact]
    public void Eject_ClearsTheAmbiguousClosureProblem_ForAConsumerSuppliedContractTheSameWayAsABuiltInOne()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder
            .ScanAssembly(typeof(AmbiguousClosureTests).Assembly)
            .RequireUniqueClosure(typeof(IClosureTestExportStrategy<>))
            .Eject(typeof(SecondConflictingExportStrategy))
            .ValidateOnBuild();

        // Assert
        Assert.DoesNotContain(
            builder.DescribeRegistrations().Problems,
            p =>
                p.Kind == RegistrationProblemKind.AmbiguousClosure
                && p.Description.Contains(nameof(FirstConflictingExportStrategy))
        );
        Assert.Contains(builder.Handlers, r => r.ImplementationType == typeof(FirstConflictingExportStrategy));
    }

    [Fact]
    public void ValidateOnBuild_BuiltInHandlerContracts_AreCheckedWithoutCallingRequireUniqueClosure()
    {
        // The three built-in handler contracts (see ValidateOnBuild_DetectsAmbiguousClosure_WhenTwoImplementations...
        // above) are already covered without any RequireUniqueClosure call; this asserts the inverse - that
        // adding a consumer contract does not somehow gate that built-in check behind requiring it too.
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder
            .ScanAssembly(typeof(AmbiguousClosureTests).Assembly)
            .RequireUniqueClosure(typeof(IClosureTestExportStrategy<>))
            .ValidateOnBuild();

        // Assert
        Assert.Contains(
            builder.DescribeRegistrations().Problems,
            p => p.Kind == RegistrationProblemKind.AmbiguousClosure && p.Description.Contains(nameof(ConflictedCommand))
        );
    }

    [Fact]
    public void RequireUniqueClosure_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        var ex = Assert.Throws<ArgumentNullException>(() => builder.RequireUniqueClosure(null!));

        // Assert
        Assert.Equal("openGenericContract", ex.ParamName);
    }

    [Fact]
    public void RequireUniqueClosure_WithAClosedType_ThrowsArgumentException()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            builder.RequireUniqueClosure(typeof(IClosureTestExportStrategy<SoleExportItem>))
        );
    }

    [Fact]
    public void RequireUniqueClosure_WithANonInterfaceType_ThrowsArgumentException()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.RequireUniqueClosure(typeof(List<>)));
    }

    [Fact]
    public void RequireUniqueClosure_AddsToUniqueClosureContracts()
    {
        // Arrange
        var builder = new InterlocutorBuilder();

        // Act
        builder.RequireUniqueClosure(typeof(IClosureTestExportStrategy<>));

        // Assert
        Assert.Contains(typeof(IClosureTestExportStrategy<>), builder.UniqueClosureContracts);
    }

    private sealed record ConflictedCommand : ICommand<Outcome>;

    private sealed class ConflictedCommandHandlerA : ICommandHandler<ConflictedCommand, Outcome>
    {
        public Task<Outcome> HandleAsync(ConflictedCommand command, CancellationToken ct) => Task.FromResult(default(Outcome));
    }

    private sealed class ConflictedCommandHandlerB : ICommandHandler<ConflictedCommand, Outcome>
    {
        public Task<Outcome> HandleAsync(ConflictedCommand command, CancellationToken ct) => Task.FromResult(default(Outcome));
    }

    private sealed record ClosureTestNotification : INotification;

    // Both handlers below are discovered only by ScanAssembly()'s reflection-based scan - unlike
    // ConflictedCommandHandlerA/B above, neither is ever named via typeof() in this file, so Sonar's
    // static analysis can't see the DI container constructing them and flags the constructors as unused.
#pragma warning disable S1144 // Resolved only via AddInterlocutor()'s assembly scanning, invisible to static analysis.
    private sealed class FirstClosureTestNotificationHandler : INotificationHandler<ClosureTestNotification>
    {
        public Task HandleAsync(ClosureTestNotification notification, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class SecondClosureTestNotificationHandler : INotificationHandler<ClosureTestNotification>
    {
        public Task HandleAsync(ClosureTestNotification notification, CancellationToken ct) => Task.CompletedTask;
    }
#pragma warning restore S1144

    // Deliberately empty: type-identity markers for this test's generic-constraint shape, not data holders.
#pragma warning disable S2094
    private sealed record SoleExportItem;
#pragma warning restore S2094

    private sealed class SoleExportStrategy : IClosureTestExportStrategy<SoleExportItem>
    {
        public Task ExportAsync(SoleExportItem item, CancellationToken ct) => Task.CompletedTask;
    }

#pragma warning disable S2094
    private sealed record ConflictingExportItem;
#pragma warning restore S2094

    private sealed class FirstConflictingExportStrategy : IClosureTestExportStrategy<ConflictingExportItem>
    {
        public Task ExportAsync(ConflictingExportItem item, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class SecondConflictingExportStrategy : IClosureTestExportStrategy<ConflictingExportItem>
    {
        public Task ExportAsync(ConflictingExportItem item, CancellationToken ct) => Task.CompletedTask;
    }
}
