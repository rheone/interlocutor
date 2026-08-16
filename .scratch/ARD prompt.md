Create a builder pattern that helps register types against the DI system with the appropriate scope.
One for MS DI
One of Autofac


Register Generic Requests and Handlers

Include polymorphic dispatching (making all Handler interfaces are contravariant)

Equivalent to

```
public interface *Handler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(TRequest message, CancellationToken cancellationToken);
}

public interface *INotificationHandler<in TNotification>
{
    Task HandleAsync(TNotification notification, CancellationToken cancellationToken);
}

etc.

```


Include:
- `IMediator` as transient
- `ISender` as transient
- `IPublisher` as transient
Need better names for `Mediator`, `ISender`, `IPublisher` -- suggestions?

It us unclear if theses should have injections:
- `INotificationHandler`
- `ICommandHandler`
- `IVoidCommandHandler`
- `IAuditSink`
- `IPipelineBehavior`
- `IPipelineStage`
- `IPolicyEvaluator`
- `IResponseOutcomeDescriber`

Is there anything I should not be including in DI? Is there anything else I should include that I'm not?




 both Generic and Non-Generic version



 Handlers and actions priority execution

All available handlers/actions will be sorted by applying next rules:

    The handler/action has a higher priority if it belongs to the current assembly (same assembly with request) and the other is not. If none of the objects belong to the current assembly, they can be considered equal. If both objects belong to the current assembly, they can't be compared only by this criterion - compare by next rule;
    The handler/action has a higher priority if it belongs to the current/child request namespace and the other is not. If both objects belong to the current/child request namespace, they can be considered equal. If none of the objects belong to the current/child request namespace, they can't be compared by this criterion - compare by next rule;
    The handler/action has a higher priority if it namespace is part of the current location (request namespace) and the other is not. If both objects are part of the current location, the closest has higher priority. If none of the objects are part of the current location, they can be considered equal.

Override handler/action

Create a new handler / action that inherits the handler / action that you want to override, and save it in accordance with the priority rules.


All need tests both unit an integration
