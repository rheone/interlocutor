namespace Interlocutor.FluentValidation.Tests;

using Interlocutor.Abstractions.Commands;
using Interlocutor.Abstractions.Pipeline;

/// <summary>Test command for validation behavior testing.</summary>
internal record TestRequest(string Name, int Age) : ICommand<string>;
