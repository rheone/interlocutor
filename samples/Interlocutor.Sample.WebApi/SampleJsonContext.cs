namespace Interlocutor.Sample.WebApi;

using System.Text.Json.Serialization;
using Interlocutor.CommonKernel.ResultTypes;
using Interlocutor.Sample.Handlers.Tags;
using Interlocutor.Sample.Handlers.Todos;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Source-generated <see cref="JsonSerializerContext"/> for every request and response body this sample's
/// minimal API endpoints read or write, wired in by <c>Program</c> via <c>ConfigureHttpJsonOptions</c>.
/// </summary>
/// <remarks>
/// <para>
/// Publishing trimmed sets <c>JsonSerializer.IsReflectionEnabledByDefault=false</c>, which removes the
/// reflection-based resolver entirely - the chain then contains only what is registered here. Anything
/// missing does not degrade gracefully; it throws <see cref="System.NotSupportedException"/> at serialization
/// time, which surfaces as a 500 on an endpoint that otherwise did its work correctly.
/// </para>
/// <para>
/// That makes the failure-body types as load-bearing as the success ones, and easy to overlook because
/// nothing references them explicitly: an endpoint returns them only through
/// <c>ToMinimalApiResult()</c>. <see cref="ProblemDetails"/> covers the conflict, validation, timeout, and
/// failure mappings; <see cref="Success"/> is the body of a void command's success case. A
/// <c>NotFound</c> maps to its bare message string, which needs no entry.
/// </para>
/// </remarks>
[JsonSerializable(typeof(CreateTodoRequest))]
[JsonSerializable(typeof(TodoItem))]
[JsonSerializable(typeof(TagItem))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(Success))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
internal sealed partial class SampleJsonContext : JsonSerializerContext;
