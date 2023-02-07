using Amazon.DynamoDBv2;
using Microsoft.Extensions.DependencyInjection;
using PrefMan.Core.Interfaces;
using PrefMan.Infrastructure.Repository;
using PrefMan.Infrastructure.Services;

namespace PrefMan.Infrastructure
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddMockServices(this IServiceCollection services)
        {
            // TODO

            return services;
        }

        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            services.AddSingleton<IAmazonDynamoDB>(new AmazonDynamoDBClient());
            services.AddSingleton<IPreferenceMetadataRepository, PreferenceMetadataRepository>();
            services.AddSingleton<IUserPreferencesRepository, UserPreferencesRepository>();
            services.AddSingleton<IPreferencesService, PreferencesService>();

            return services;
        }
    }
}
