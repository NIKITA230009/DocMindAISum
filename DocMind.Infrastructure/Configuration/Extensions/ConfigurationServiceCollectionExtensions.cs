using DocMind.Infrastructure.Configuration.Models;
using DocMind.Infrastructure.Configuration.Providers;
using DocMind.Infrastructure.Configuration.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DocMind.Infrastructure.Configuration.Extensions;

public static class ConfigurationServiceCollectionExtensions
{
    public static IServiceCollection AddConfigurationServices(
        this IServiceCollection services)
    {
        // Register configuration providers - use fully qualified name to avoid conflict
        services.AddSingleton<Providers.IConfigurationProvider, JsonFileConfigurationProvider>();
        
        // Register configuration service
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        
        // Register options for AppSettings (will be populated by ConfigurationService)
        services.AddOptions<AppSettings>();
        services.AddOptions<DatabaseSettings>();
        services.AddOptions<AiSettings>();
        services.AddOptions<UiSettings>();
        services.AddOptions<LoggingSettings>();
        services.AddOptions<FeatureSettings>();
        
        // Register options monitor for reactive configuration
        services.AddSingleton<IOptionsMonitor<AppSettings>>(provider =>
        {
            var configService = provider.GetRequiredService<IConfigurationService>();
            return new OptionsMonitorWrapper(configService);
        });
        
        return services;
    }
    
    public static IServiceCollection AddConfigurationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // First add the basic services
        services.AddConfigurationServices();
        
        // If configuration is provided, bind it
        if (configuration != null)
        {
            // We'll let the ConfigurationService handle loading from configuration
            // The configuration parameter can be used for additional setup if needed
        }
        
        return services;
    }
    
    private class OptionsMonitorWrapper : IOptionsMonitor<AppSettings>
    {
        private readonly IConfigurationService _configService;
        
        public OptionsMonitorWrapper(IConfigurationService configService)
        {
            _configService = configService;
        }
        
        public AppSettings CurrentValue => _configService.CurrentSettings;
        
        public AppSettings Get(string name) => CurrentValue;
        
        public IDisposable OnChange(Action<AppSettings, string> listener)
        {
            return _configService.SettingsChanged.Subscribe(settings => listener(settings, string.Empty));
        }
    }
}