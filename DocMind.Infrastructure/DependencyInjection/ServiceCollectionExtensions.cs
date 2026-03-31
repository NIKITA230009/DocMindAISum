using DocMind.Infrastructure.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DocMind.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDocMindServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add configuration services
        services.AddConfigurationServices(configuration);
        
        // Add logging - Serilog will be configured separately
        services.AddLogging();
        
        // Add infrastructure services will be added here
        // services.AddInfrastructureServices();
        
        // Add data access services will be added here
        // services.AddDataAccess(configuration);
        
        // Add business services will be added here
        // services.AddBusinessServices();
        
        // Add AI services will be added here
        // services.AddAiServices(configuration);
        
        // Add UI services will be added here
        // services.AddUiServices();
        
        return services;
    }
    
    public static IServiceCollection AddDocMindServices(
        this IServiceCollection services)
    {
        // Simplified overload without configuration
        return services.AddDocMindServices(null);
    }
}