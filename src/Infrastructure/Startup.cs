using Microsoft.Extensions.DependencyInjection;
namespace PrefMan.Infrastructure;

public static class Startup
{
    private static ServiceProvider? _serviceProvider;

    public static ServiceProvider ServiceProvider
    {
        get
        {
            if (_serviceProvider == null)
            {
                _serviceProvider = InitializeServiceProvider();
            }

            return _serviceProvider;
        }
        private set => _serviceProvider = value;
    }

    private static ServiceProvider InitializeServiceProvider()
    {
        var services = new ServiceCollection();

        /*
        var logger = new LoggerConfiguration()
            .WriteTo.Console(new RenderedCompactJsonFormatter())
            .CreateLogger();
        
        services.AddLogging(builder =>
        {
            builder.AddSerilog(logger);
        });
        */

        if (Environment.GetEnvironmentVariable("USE_MOCKS") == "true")
        {
            services.AddMockServices();
        }
        else
        {
            services.AddInfrastructureServices();
        }

        return services.BuildServiceProvider();
    }
}