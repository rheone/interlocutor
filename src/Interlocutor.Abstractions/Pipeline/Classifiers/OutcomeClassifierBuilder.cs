namespace Interlocutor.Abstractions.Pipeline.Classifiers;

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Builds an immutable <see cref="IOutcomeClassifier"/> that labels operation outcomes for
/// logging/auditing. Seeded with all kernel types via open-generic matching; consumers
/// may register additional custom types.
/// </summary>
/// <remarks>
/// <para>
/// The builder collects type registrations and predicates, then produces an immutable classifier.
/// It is pre-seeded with all Interlocutor.CommonKernel result types so new arities cannot silently
/// regress labeling behavior.
/// </para>
/// <para>
/// The builder is a pure label registry - a type or predicate maps to a display label, nothing
/// more. It carries no success/failure judgment: <see cref="IOutcomeClassifier.GetLabel"/> falls
/// back leniently to the runtime type name for anything unregistered, and never throws.
/// </para>
/// </remarks>
public sealed class OutcomeClassifierBuilder
{
    private static readonly Lock _lockObject = new();
    private readonly Dictionary<Type, OutcomeClassification> _classifications = [];
    private readonly List<(Func<object?, bool> Predicate, string Label)> _predicates = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="OutcomeClassifierBuilder"/> class, pre-seeded with all Interlocutor.CommonKernel case types.
    /// </summary>
    public OutcomeClassifierBuilder()
    {
        // Seed with kernel types using open-generic matching where applicable.
        // This ensures new union arities cannot silently regress.
        this.RegisterType<Success>("Success");
        this.RegisterType<NotFound>("NotFound");
        this.RegisterType<Forbidden>("Forbidden");
        this.RegisterType<Conflict>("Conflict");
        this.RegisterType<ValidationErrors>("ValidationError");
        this.RegisterType<Failures>("Failure");
        this.RegisterType<Conflicts>("Conflict");
        this.RegisterType<Failure>("Failure");
        this.RegisterType<Cancelled>("Cancelled");
        this.RegisterType<Timeout>("Timeout");
        this.RegisterType<RateLimited>("RateLimited");

        // Register generic variants via predicate to handle open-ended shapes.
        // This covers ValidationError<TErrorCode>, ValidationErrors<TErrorCode>, and
        // any future generic result types.
        this.RegisterPredicate(
            response => response?.GetType().IsGenericType == true && response.GetType().Name.StartsWith("ValidationError"),
            "ValidationError"
        );
    }

    /// <summary>
    /// Gets the shared default classifier instance, pre-seeded with all kernel types.
    /// </summary>
    /// <value>
    /// The shared default classifier instance, pre-seeded with all kernel types.
    /// </value>
    public static IOutcomeClassifier DefaultClassifier
    {
        get
        {
            if (field is null)
            {
                lock (_lockObject)
                {
                    field ??= new OutcomeClassifierBuilder().Build();
                }
            }

            return field;
        }
    }

    /// <summary>
    /// Registers a type's display label.
    /// </summary>
    /// <typeparam name="T">The type to register.</typeparam>
    /// <param name="label">The human-readable label for this type.</param>
    /// <returns>This builder for method chaining.</returns>
    public OutcomeClassifierBuilder RegisterType<T>(string label)
        where T : notnull
    {
        this._classifications[typeof(T)] = new OutcomeClassification(label);
        return this;
    }

    /// <summary>
    /// Registers a type's display label, by runtime <see cref="Type"/>.
    /// </summary>
    /// <param name="type">The type to register.</param>
    /// <param name="label">The human-readable label for this type.</param>
    /// <returns>This builder for method chaining.</returns>
    public OutcomeClassifierBuilder RegisterType(Type type, string label)
    {
        ArgumentNullException.ThrowIfNull(type);
        this._classifications[type] = new OutcomeClassification(label);
        return this;
    }

    /// <summary>
    /// Registers a predicate-based label for outcome types that cannot be expressed by type alone.
    /// </summary>
    /// <remarks>
    /// Predicates are evaluated in registration order when a response's type is not found in the
    /// type registration table. The first predicate to return true wins; if none match, <see cref="IOutcomeClassifier.GetLabel"/>
    /// falls back to the runtime type name.
    /// </remarks>
    /// <param name="predicate">A function that returns true if the response matches this label.</param>
    /// <param name="label">The human-readable label for matching outcomes.</param>
    /// <returns>This builder for method chaining.</returns>
    public OutcomeClassifierBuilder RegisterPredicate(Func<object?, bool> predicate, string label)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(label);
        this._predicates.Add((predicate, label));
        return this;
    }

    /// <summary>
    /// Builds an immutable classifier from the registrations.
    /// </summary>
    /// <returns>An immutable classifier that applies all registered labels.</returns>
    public IOutcomeClassifier Build() =>
        new DefaultOutcomeClassifier(this._classifications.ToImmutableDictionary(), this._predicates.ToImmutableList());

    /// <summary>
    /// Internal immutable classifier implementation produced by the builder.
    /// </summary>
    private sealed class DefaultOutcomeClassifier : IOutcomeClassifier
    {
        private readonly ImmutableDictionary<Type, OutcomeClassification> _classifications;
        private readonly ImmutableList<(Func<object?, bool> Predicate, string Label)> _predicates;

        internal DefaultOutcomeClassifier(
            ImmutableDictionary<Type, OutcomeClassification> classifications,
            ImmutableList<(Func<object?, bool> Predicate, string Label)> predicates
        )
        {
            this._classifications = classifications;
            this._predicates = predicates;
        }

        public string GetLabel(object? response)
        {
            var unwrapped = Unwrap(response);
            var type = unwrapped?.GetType();

            // Check type registrations first.
            if (type is not null && this._classifications.TryGetValue(type, out var classification))
            {
                return classification.Label;
            }

            // Check predicates in order.
            foreach (var (predicate, label) in this._predicates)
            {
                if (predicate(unwrapped))
                {
                    return label;
                }
            }

            // A null Value alone can't say whether this is a legitimate null case payload or a genuinely
            // unset union - ask the union itself before falling back to the generic "None"/"Unknown" labels.
            if (unwrapped is null && response.AsOutcomeCaseActiveType() is { } activeCaseType)
            {
                return activeCaseType.Name;
            }

            // Null/None outcome (a genuinely unset union, or a non-union null response).
            if (unwrapped is null)
            {
                return "None";
            }

            // Fallback: use runtime type name for unregistered types.
            return type?.Name ?? "Unknown";
        }

        // Unwrap compiler-generated union types to access their underlying Value.
        private static object? Unwrap(object? response) => response is IUnion union ? union.Value : response;
    }
}

#pragma warning disable SA1402 // Allowing this small support type to stay co-located with the builder that owns it

/// <summary>
/// Describes an outcome type's registered display label.
/// </summary>
internal sealed record class OutcomeClassification(string Label);

#pragma warning restore SA1402
