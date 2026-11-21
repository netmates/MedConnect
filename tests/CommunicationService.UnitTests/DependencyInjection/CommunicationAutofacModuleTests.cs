using Autofac;
using Autofac.Extensions.DependencyInjection;
using CommunicationService.Common.Consuming;
using CommunicationService.Common.DependencyInjection;
using MedConnect.Shared.Consuming;
using MedConnect.Shared.Events;
using Microsoft.Extensions.DependencyInjection;

namespace CommunicationService.UnitTests.DependencyInjection;

public class CommunicationAutofacModuleTests
{
    [Fact]
    public void Resolve_ReturnsMongoRetryDecorator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        var builder = new ContainerBuilder();
        builder.Populate(services);
        builder.RegisterModule<CommunicationAutofacModule>();
        builder.RegisterType<UnusedScopeFactory>().As<IServiceScopeFactory>().SingleInstance();

        using var container = builder.Build();

        // Act
        var created = container.Resolve<IIntegrationEventHandler<AppointmentCreatedPayload>>();
        var renamed = container.Resolve<IIntegrationEventHandler<ParticipantNameUpdatedPayload>>();

        // Assert
        Assert.IsType<MongoRetryIntegrationEventHandler<AppointmentCreatedPayload>>(created);
        Assert.IsType<MongoRetryIntegrationEventHandler<ParticipantNameUpdatedPayload>>(renamed);
    }

    private sealed class UnusedScopeFactory : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => throw new NotSupportedException();
    }
}
