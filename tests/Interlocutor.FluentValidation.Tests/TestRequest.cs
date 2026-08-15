namespace Interlocutor.FluentValidation.Tests;

using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Pipeline;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>Test command for validation behavior testing.</summary>
internal record TestRequest(string Name, int Age) : ICommand<OperationOutcome<string, Failure>>;
