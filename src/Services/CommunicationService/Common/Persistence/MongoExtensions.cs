using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace CommunicationService.Common.Persistence;

public static class MongoExtensions
{
    public static IServiceCollection AddMongo(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MongoOptions>(configuration.GetSection(MongoOptions.SectionName));

        services.AddSingleton<IMongoClient>(sp =>
        {
            var cs = configuration.GetConnectionString(MongoOptions.ConnectionStringName)
                ?? throw new InvalidOperationException($"ConnectionStrings:{MongoOptions.ConnectionStringName} не задан.");
            return new MongoClient(cs);
        });

        services.AddSingleton(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            var options = sp.GetRequiredService<IOptions<MongoOptions>>().Value;

            if (string.IsNullOrWhiteSpace(options.Database))
                throw new InvalidOperationException($"{MongoOptions.SectionName}:Database не задан.");

            return client.GetDatabase(options.Database);
        });

        return services;
    }
}
