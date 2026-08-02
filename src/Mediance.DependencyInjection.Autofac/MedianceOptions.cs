namespace Mediance.DependencyInjection.Autofac;

using System.Reflection;
using Mediance.Abstractions;
using Mediance.Abstractions.Core;
using Mediance.Abstractions.Notifications;
using Mediance.Abstractions.Pipeline;

/// <summary>
/// Configuration options for Mediance registration via <see cref="MedianceAutofacExtensions.AddMediance(global::Autofac.ContainerBuilder, MedianceOptions)"/>.
/// </summary>
/// <remarks>
/// Allows customization of handler assembly scanning, pipeline behaviors, outcome describer registration,
/// and notification publish strategy.
/// </remarks>
public class MedianceOptions
{
    /// <summary>
    /// Gets or sets the assemblies to scan for handler implementations.
    /// If not set, defaults to the entry assembly.
    /// </summary>
    /// <value>
    /// <placeholder>The assemblies to scan for handler implementations.
    /// If not set, defaults to the entry assembly.</placeholder>
    /// </value>
    public Assembly[] AssembliesToScan { get; set; } = [Assembly.GetEntryAssembly()!];

    /// <summary>
    /// Gets or sets an optional predicate to filter which assemblies are scanned.
    /// If set, only assemblies for which this predicate returns true will be scanned.
    /// </summary>
    /// <value>
    /// <placeholder>An optional predicate to filter which assemblies are scanned.
    /// If set, only assemblies for which this predicate returns true will be scanned.</placeholder>
    /// </value>
    public Func<Assembly, bool>? AssemblyFilter { get; set; }

    /// <summary>
    /// Gets or sets the default notification publish strategy.
    /// Defaults to <see cref="PublishStrategy.Sequential"/>.
    /// </summary>
    /// <value>
    /// <placeholder>The default notification publish strategy.
    /// Defaults to <see cref="PublishStrategy.Sequential"/>.</placeholder>
    /// </value>
    public PublishStrategy DefaultPublishStrategy { get; set; } = PublishStrategy.Sequential;

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets whether to enable the default behavior chain.
    /// When true, registers LoggingBehavior, AuditingBehavior, AuthorizationBehavior,
    /// ValidationBehavior, and TransactionBehavior in canonical order.
    /// Defaults to true.
    /// </summary>
    /// <value>
    /// <placeholder>Whether to enable the default behavior chain.
    /// When true, registers LoggingBehavior, AuditingBehavior, AuthorizationBehavior,
    /// ValidationBehavior, and TransactionBehavior in canonical order.
    /// Defaults to true.</placeholder>
    /// </value>
    public bool EnableDefaultBehaviors { get; set; } = true;

    /// <summary>
    /// Gets or sets the type of <see cref="IResponseOutcomeDescriber"/> to register.
    /// Defaults to <see cref="DefaultResponseOutcomeDescriber"/>.
    /// </summary>
    /// <value>
    /// <placeholder>The type of <see cref="IResponseOutcomeDescriber"/> to register.
    /// Defaults to <see cref="DefaultResponseOutcomeDescriber"/>.</placeholder>
    /// </value>
    public Type ResponseOutcomeDescriberType { get; set; } = typeof(DefaultResponseOutcomeDescriber);

    /// <summary>
    /// Gets custom outcome types that should be registered with the describer.
    /// Each entry is a tuple of (outcome type, outcome case types).
    /// </summary>
    /// <value>
    /// <placeholder>Custom outcome types that should be registered with the describer.
    /// Each entry is a tuple of (outcome type, outcome case types).</placeholder>
    /// </value>
    public List<Type> CustomOutcomeTypes { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets whether to run startup validation on registered handlers and behaviors.
    /// Defaults to true.
    /// </summary>
    /// <value>
    /// <placeholder>Whether to run startup validation on registered handlers and behaviors.
    /// Defaults to true.</placeholder>
    /// </value>
    public bool EnableStartupValidation { get; set; } = true;
}
