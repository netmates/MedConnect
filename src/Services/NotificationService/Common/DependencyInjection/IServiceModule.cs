namespace NotificationService.Common.DependencyInjection;

public interface IServiceModule
{
    public void Register(IServiceCollection services, IConfiguration configuration);
}
