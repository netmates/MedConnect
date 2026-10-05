namespace NotificationService.Common.DependencyInjection;

public interface IServiceModule
{
    void Register(IServiceCollection services, IConfiguration configuration);
}
