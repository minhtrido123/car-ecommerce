using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure;

public static class CachingExtensions
{
    public static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration)
    {
        // try
        // {
        var redisConnectionString = configuration.GetValue<string>("Cache:RedisConnectionString") ?? "localhost:6379";
        var kafkaBrokers = configuration.GetValue<string>("Kafka:Brokers") ?? "localhost:9092";
        var kafkaTopic = configuration.GetValue<string>("Kafka:InvalidationTopic") ?? "cache.invalidation";
        var kafkaGroupId = configuration.GetValue<string>("Kafka:GroupId") ?? "cache-worker";
        var kafkaInventoryTopic = configuration.GetValue<string>("Kafka:InventoryTopic") ?? "inventory.updated";

        services.AddSingleton(new Lazy<ICacheService>(() =>
        {
            try
            {
                return new RedisCacheService(redisConnectionString);
            }
            catch
            {
                Console.WriteLine("Cache can't be reached, running with no cache");
                return null!;
            }
        }));
        services.AddSingleton(new Lazy<RedisCacheService>(() =>
        {
            try
            {
                return new RedisCacheService(redisConnectionString);
            }
            catch
            {
                // Console.WriteLine("Cache can't be reached, running with no cache");
                return null!;
            }
        }));
        services.AddSingleton(sp =>
        {
            try
            {
                return new Lazy<IKafkaProducer>(() =>
                {
                    var logger = sp.GetRequiredService<ILogger<KafkaProducer>>();
                    return new KafkaProducer(kafkaBrokers, kafkaTopic, kafkaInventoryTopic, logger);
                });
            }
            catch
            {
                Console.WriteLine("Kafka can't be reached, running with no message queue");
                return null!;
            }
        }
        );
        // services.AddSingleton<ICacheService>(sp => sp.GetRequiredService<RedisCacheService>());
        // services.AddSingleton<ICacheWorker>(sp =>
        // {
        //     var cache = sp.GetRequiredService<RedisCacheService>();
        //     var logger = sp.GetRequiredService<ILogger<CacheWorker>>();
        //     return new CacheWorker(cache, kafkaBrokers, kafkaTopic, kafkaGroupId, logger);
        // });
        // services.AddHostedService(sp => sp.GetRequiredService<ICacheWorker>());
        return services;
    }
    // catch
    // {
    //     Console.WriteLine("Cache can't be reached, running with no cache");
    // }
    // return services;
}
