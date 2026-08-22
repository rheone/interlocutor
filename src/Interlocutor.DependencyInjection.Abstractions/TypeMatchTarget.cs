namespace Interlocutor.DependencyInjection.Abstractions;

/// <summary>
/// What role a predicate-based type-matching rule (<see cref="InterlocutorBuilder.AddTypeMatch"/>)
/// registers each matching type as.
/// </summary>
public enum TypeMatchTarget
{
    /// <summary>
    /// Register each matching type the same way <see cref="InterlocutorBuilder.AddHandler"/> would - one
    /// <see cref="RegistrationKind.Handler"/> entry per mediator handler contract interface it implements.
    /// </summary>
    Handler,

    /// <summary>
    /// Register each matching type against the open <see cref="Interlocutor.Abstractions.Pipeline.IPipelineBehavior{TRequest,TResponse}"/>
    /// contract, the same way <see cref="InterlocutorPipelineBuilder.AddBehavior"/> would.
    /// </summary>
    Behavior,
}
