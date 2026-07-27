# Task 2+3 Report

## Summary
- **Files created:** 30 (22 from Abstractions.cs, 8 from Behaviors.cs)
- **Files deleted:** 1 (`Abstractions.cs` — `Behaviors.cs` did not exist, likely already removed or never tracked)

## Issues
- `Behaviors.cs` was not present in the repository (not tracked by git, file not found). No action needed.
- Build will not pass — expected per task constraints.

## Directory structure created
```
src/Mediance.Abstractions/
├── Core/          (6 files: ISender, IPublisher, IMediator, PublishStrategy, IRequestContextAccessor, RequestContext)
├── Commands/      (5 files: ICommand, IEnumerableCommand, IBulkCommand, ICommandHandler, ResourceCommandHandler)
├── Queries/       (4 files: IQuery, IEnumerableQuery, IPagedQuery, IQueryHandler)
├── Notifications/ (2 files: INotification, INotificationHandler)
├── Pipeline/      (9 files: IPipelineBehavior + 8 from Behaviors.cs)
└── Markers/       (4 files: ITransactionalRequest, IExternalStoreRequest, IAuditableRequest, IRequiresPolicy)
```
