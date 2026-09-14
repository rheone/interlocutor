namespace Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// A single validation failure message attributed to the property/field it applies to, if any.
/// </summary>
/// <param name="PropertyName">
/// The name of the property/field that failed, or <see langword="null"/> when the failure isn't tied to a
/// specific property - a cross-field or whole-object rule, for example.
/// </param>
/// <param name="Message">The human-readable validation failure message.</param>
/// <remarks>
/// <para>
/// The entry type behind <see cref="IFieldValidationFailureCase.FieldMessages"/> - kept as its own record
/// struct rather than a bare value tuple so the two fields carry names and XML documentation a consumer can
/// discover, and so a future field (an error code, for example) can be added without breaking every call site.
/// </para>
/// </remarks>
public readonly record struct FieldValidationMessage(string? PropertyName, string Message);
