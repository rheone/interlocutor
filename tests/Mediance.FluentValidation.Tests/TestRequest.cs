namespace Mediance.FluentValidation.Tests;

using Mediance.Abstractions.Pipeline;

/// <summary>Test command for validation behavior testing.</summary>
internal record TestRequest(string Name, int Age) : ICommand<string>;
