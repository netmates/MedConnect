using Autofac;
using CommunicationService.Common.Consuming;
using MedConnect.Shared.Consuming;

namespace CommunicationService.Common.DependencyInjection;

public sealed class CommunicationAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Хендлер живёт вместе с QueueConsumer: один экземпляр на процесс.
        builder.RegisterAssemblyTypes(typeof(CommunicationAutofacModule).Assembly)
            .AsClosedTypesOf(typeof(IIntegrationEventHandler<>))
            .SingleInstance();

        builder.RegisterGenericDecorator(
            typeof(MongoRetryIntegrationEventHandler<>),
            typeof(IIntegrationEventHandler<>));
    }
}
