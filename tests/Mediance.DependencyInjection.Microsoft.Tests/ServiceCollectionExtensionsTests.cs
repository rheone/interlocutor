using Mediance.Abstractions;
using Mediance.Abstractions.Commands;
using Mediance.Abstractions.Core;
using Mediance.Abstractions.Notifications;
using Mediance.Abstractions.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Mediance.DependencyInjection.Microsoft.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMediance_RegistersMediatorAsScoped()
    {
        var services = new ServiceCollection();
        services.AddMediance(typeof(ServiceCollectionExtensionsTests).Assembly);

        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IMediator));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor!.Lifetime);
    }
}
