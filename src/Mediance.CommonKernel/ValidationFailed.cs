namespace Mediance.CommonKernel;

public sealed record ValidationFailed(
    IReadOnlyDictionary<string, IReadOnlyList<(string Code, string Message)>> errors);
