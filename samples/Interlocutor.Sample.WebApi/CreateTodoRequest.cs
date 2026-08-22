/// <summary>The request body for <c>POST /todos</c>.</summary>
/// <param name="Title">The new to-do item's title.</param>
internal sealed record CreateTodoRequest(string Title);
