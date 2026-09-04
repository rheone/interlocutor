namespace TodoTasks.Api.Tests.Http;

using Microsoft.AspNetCore.Mvc.Testing;

/// <summary>
/// Hosts the real <c>TodoTasks.Api</c> in-process for end-to-end tests - the exact same
/// <c>Program.cs</c> that <c>dotnet run</c> starts, including its own real Microsoft DI container, EF
/// Core schema creation, and pipeline registration.
/// </summary>
/// <remarks>
/// Because <c>Program.cs</c> builds its own in-memory SQLite connection internally (a fresh
/// <c>Data Source=:memory:</c> <c>SqliteConnection</c> per app instance) rather than reading a real
/// connection string, every <see cref="TodoTasksApiFactory"/> instance gets a completely fresh,
/// isolated database for free - no test-specific configuration override is needed to get that
/// isolation. Feature tickets that do need to override a service for a specific test (a fake clock, a
/// seeded dataset) can do so by overriding <see cref="WebApplicationFactory{TEntryPoint}.ConfigureWebHost"/>
/// in a subclass, or by adding it here if the override is broadly useful across many tests.
/// </remarks>
public sealed class TodoTasksApiFactory : WebApplicationFactory<Program>;
