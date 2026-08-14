namespace Interlocutor.DependencyInjection.Autofac;

using System.Reflection;
using Interlocutor.Abstractions;
using Interlocutor.Abstractions.Core;
using Interlocutor.Abstractions.Notifications;
using Interlocutor.Abstractions.Pipeline;

/// <summary>
/// Configuration options for Interlocutor registration via <see cref="InterlocutorAutofacExtensions.AddInterlocutor(global::Autofac.ContainerBuilder, InterlocutorOptions)"/>.
/// </summary>
/// <remarks>
/// Allows customization of handler assembly scanning, pipeline behaviors, outcome describer registration,
/// and notification publish strategy.
/// </remarks>
public class InterlocutorOptions
{
    /// <summary>
    /// Gets or sets the assemblies to scan for handler implementations.
    /// If not set, defaults to the entry assembly.
    /// </summary>
    /// <value>
    /// The assemblies to scan for handler implementations.
    /// If not set, defaults to the entry assembly.
    /// </value>
    public Assembly[] AssembliesToScan { get; set; } = [Assembly.GetEntryAssembly()!];

    /// <summary>
    /// Gets or sets an optional predicate to filter which assemblies are scanned.
    /// If set, only assemblies for which this predicate returns true will be scanned.
    /// </summary>
    /// <value>
    /// An optional predicate to filter which assemblies are scanned.
    /// If set, only assemblies for which this predicate returns true will be scanned.
    /// </value>
    public Func<Assembly, bool>? AssemblyFilter { get; set; }

    /// <summary>
    /// Gets or sets the default notification publish strategy.
    /// Defaults to <see cref="PublishStrategy.Sequential"/>.
    /// </summary>
    /// <value>
    /// The default notification publish strategy.
    /// Defaults to <see cref="PublishStrategy.Sequential"/>.
    /// </value>
    public PublishStrategy DefaultPublishStrategy { get; set; } = PublishStrategy.Sequential;

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets whether to enable the default behavior chain.
    /// When true, registers LoggingBehavior, AuditingBehavior, AuthorizationBehavior,
    /// ValidationBehavior, and TransactionBehavior in canonical order.
    /// Defaults to true.
    /// </summary>
    /// <value>
    /// Whether to enable the default behavior chain.
    /// When true, registers LoggingBehavior, AuditingBehavior, AuthorizationBehavior,
    /// ValidationBehavior, and TransactionBehavior in canonical order.
    /// Defaults to true.
    /// </value>
    public bool EnableDefaultBehaviors { get; set; } = true;

    /// <summary>
    /// Gets or sets the type of <see cref="IResponseOutcomeDescriber"/> to register.
    /// Defaults to <see cref="DefaultResponseOutcomeDescriber"/>.
    /// </summary>
    /// <value>
    /// The type of <see cref="IResponseOutcomeDescriber"/> to register.
    /// Defaults to <see cref="DefaultResponseOutcomeDescriber"/>.
    /// </value>
    public Type ResponseOutcomeDescriberType { get; set; } = typeof(DefaultResponseOutcomeDescriber);

    /// <summary>
    /// Gets custom outcome types that should be registered with the describer.
    /// Each entry is a tuple of (outcome type, outcome case types).
    /// </summary>
    /// <value>
    /// Custom outcome types that should be registered with the describer.
    /// Each entry is a tuple of (outcome type, outcome case types).
    /// </value>
    public List<Type> CustomOutcomeTypes { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets whether to run startup validation on registered handlers and behaviors.
    /// Defaults to true.
    /// </summary>
    /// <value>
    /// Whether to run startup validation on registered handlers and behaviors.
    /// Defaults to true.
    /// </value>
    public bool EnableStartupValidation { get; set; } = true;
}
