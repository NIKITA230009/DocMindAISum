# Dependency Injection Container Configuration

## Current DI Setup Analysis

**Current Configuration** (in App.axaml.cs):
```csharp
private void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton<MainWindowViewModel>();
    services.AddSingleton<IDocumentService, WordDocumentService>();
    services.AddSingleton<IDocumentCacheService, LiteDbDocumentCache>();
    services.AddRefitClient<IAiApi>()
        .ConfigureHttpClient(c => { /* ... */ });
    services.AddSingleton<IAiService, AiService>();
    services.AddSingleton<IQueryHistoryService, LiteDbQueryHistoryService>();
    services.AddSingleton<ILocalAiService, LLamaSharpAiService>();
}
```

**Issues Identified**:
1. Mixed lifetime scopes (all singletons)
2. Configuration hardcoded in UI layer
3. No separation of DI configuration by layer
4. Missing configuration options pattern
5. No support for environment-specific configurations

## Proposed DI Configuration Structure

### 1. Layer-Specific Extension Methods

Create extension methods for each architectural layer:

```
DocMind.Infrastructure/
└── DependencyInjection/
    ├── ServiceCollectionExtensions.cs
    ├── DataServiceCollectionExtensions.cs
    ├── BusinessServiceCollectionExtensions.cs
    ├── AiServiceCollectionExtensions.cs
    └── UiServiceCollectionExtensions.cs
```

### 2. Core DI Configuration

**ServiceCollectionExtensions.cs** (Main entry point):
```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace DocMind.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDocMindServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add configuration
        services.AddConfiguration(configuration);
        
        // Add logging
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog();
        });
        
        // Add infrastructure services
        services.AddInfrastructureServices();
        
        // Add data access layer
        services.AddDataAccess(configuration);
        
        // Add business services
        services.AddBusinessServices();
        
        // Add AI services
        services.AddAiServices(configuration);
        
        // Add UI services
        services.AddUiServices();
        
        return services;
    }
    
    private static IServiceCollection AddConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AppSettings>(configuration);
        services.Configure<DatabaseSettings>(
            configuration.GetSection("Database"));
        services.Configure<AiSettings>(
            configuration.GetSection("Ai"));
        services.Configure<UiSettings>(
            configuration.GetSection("Ui"));
            
        return services;
    }
}
```

### 3. Data Access Layer DI Configuration

**DataServiceCollectionExtensions.cs**:
```csharp
using DocMind.Core.Interfaces;
using DocMind.Data.Repositories;
using DocMind.Data.Contexts;
using LiteDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DocMind.Infrastructure.DependencyInjection;

public static class DataServiceCollectionExtensions
{
    public static IServiceCollection AddDataAccess(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register LiteDB context
        services.AddSingleton<ILiteDatabaseContext>(provider =>
        {
            var settings = provider.GetRequiredService<IOptions<DatabaseSettings>>().Value;
            return new LiteDatabaseContext(settings.ConnectionString);
        });
        
        // Register repositories with scoped lifetime
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IQueryHistoryRepository, QueryHistoryRepository>();
        services.AddScoped<ICacheRepository, CacheRepository>();
        
        // Register unit of work
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        return services;
    }
}
```

### 4. Business Services Layer DI Configuration

**BusinessServiceCollectionExtensions.cs**:
```csharp
using DocMind.Core.Interfaces;
using DocMind.Services.Document;
using DocMind.Services.AI;
using DocMind.Services.Cache;
using DocMind.Services.History;
using Microsoft.Extensions.DependencyInjection;

namespace DocMind.Infrastructure.DependencyInjection;

public static class BusinessServiceCollectionExtensions
{
    public static IServiceCollection AddBusinessServices(
        this IServiceCollection services)
    {
        // Document services
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IDocumentParser, WordDocumentParser>();
        services.AddScoped<IDocumentParser, PdfDocumentParser>();
        services.AddScoped<IDocumentParser, TextDocumentParser>();
        services.AddScoped<IDocumentParserFactory, DocumentParserFactory>();
        
        // Cache services
        services.AddScoped<IDocumentCacheService, DocumentCacheService>();
        services.AddSingleton<IMemoryCacheService, MemoryCacheService>();
        
        // History services
        services.AddScoped<IQueryHistoryService, QueryHistoryService>();
        
        // Validation services
        services.AddScoped<IDocumentValidator, DocumentValidator>();
        
        return services;
    }
}
```

### 5. AI Services Layer DI Configuration

**AiServiceCollectionExtensions.cs**:
```csharp
using DocMind.Core.Interfaces;
using DocMind.Services.AI.Providers;
using DocMind.Services.AI.Factories;
using DocMind.Services.AI.Pipelines;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DocMind.Infrastructure.DependencyInjection;

public static class AiServiceCollectionExtensions
{
    public static IServiceCollection AddAiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure AI settings
        services.Configure<AiSettings>(configuration.GetSection("Ai"));
        
        // Register AI providers with strategy pattern
        services.AddSingleton<ILocalAiProvider, LLamaSharpAiProvider>();
        services.AddSingleton<IOpenAiProvider, OpenAiProvider>();
        services.AddSingleton<IAzureAiProvider, AzureAiProvider>();
        
        // Register AI service factory
        services.AddSingleton<IAiServiceFactory, AiServiceFactory>();
        
        // Register main AI service (uses factory to select provider)
        services.AddScoped<IAiService, AiService>();
        
        // Register prompt template service
        services.AddSingleton<IPromptTemplateService, PromptTemplateService>();
        
        // Register AI pipeline middleware
        services.AddSingleton<IAiPipeline, LoggingPipeline>();
        services.AddSingleton<IAiPipeline, ValidationPipeline>();
        services.AddSingleton<IAiPipeline, CachingPipeline>();
        
        // Register pipeline executor
        services.AddSingleton<IAiPipelineExecutor, AiPipelineExecutor>();
        
        return services;
    }
}
```

### 6. UI Layer DI Configuration

**UiServiceCollectionExtensions.cs**:
```csharp
using DocMind.Desktop.ViewModels;
using DocMind.Desktop.Services;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace DocMind.Infrastructure.DependencyInjection;

public static class UiServiceCollectionExtensions
{
    public static IServiceCollection AddUiServices(
        this IServiceCollection services)
    {
        // ViewModels with scoped lifetime (per window)
        services.AddScoped<MainWindowViewModel>();
        services.AddScoped<DocumentViewModel>();
        services.AddScoped<SettingsViewModel>();
        services.AddScoped<HistoryViewModel>();
        
        // UI services
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IFilePickerService, FilePickerService>();
        services.AddSingleton<INotificationService, NotificationService>();
        
        // Theme and styling services
        services.AddSingleton<IThemeService, ThemeService>();
        
        return services;
    }
}
```

### 7. Infrastructure Services DI Configuration

**InfrastructureServiceCollectionExtensions.cs**:
```csharp
using DocMind.Infrastructure.Configuration;
using DocMind.Infrastructure.Logging;
using DocMind.Infrastructure.FileSystem;
using Microsoft.Extensions.DependencyInjection;

namespace DocMind.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services)
    {
        // Configuration
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        
        // Logging
        services.AddSingleton<ILoggingService, SerilogLoggingService>();
        
        // File system
        services.AddSingleton<IFileSystemService, FileSystemService>();
        
        // Security
        services.AddSingleton<ISecurityService, SecurityService>();
        
        // Performance monitoring
        services.AddSingleton<IPerformanceMonitor, PerformanceMonitor>();
        
        return services;
    }
}
```

### 8. Updated App.axaml.cs Configuration

**Simplified App Configuration**:
```csharp
public override void OnFrameworkInitializationCompleted()
{
    // Build configuration
    var configuration = new ConfigurationBuilder()
        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true)
        .AddEnvironmentVariables()
        .Build();
    
    // Configure services
    var services = new ServiceCollection();
    services.AddDocMindServices(configuration);
    
    _services = services.BuildServiceProvider();
    
    // Initialize application
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        // Get required services
        var cacheService = _services.GetRequiredService<IDocumentCacheService>();
        var logger = _services.GetRequiredService<ILogger<App>>();
        
        // Background cleanup
        _ = Task.Run(async () => 
        {
            try
            {
                await cacheService.CleanupOldEntriesAsync(TimeSpan.FromDays(30));
                logger.LogInformation("Cache cleanup completed");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cache cleanup failed");
            }
        });
        
        // Create main window
        desktop.MainWindow = new MainWindow
        {
            DataContext = _services.GetRequiredService<MainWindowViewModel>()
        };
    }
    
    base.OnFrameworkInitializationCompleted();
}
```

### 9. Service Lifetimes Guidelines

| Service Type | Lifetime | Reason |
|-------------|----------|--------|
| **Singleton** | Application lifetime | Configuration, logging, AI model weights |
| **Scoped** | Per window/request | ViewModels, database contexts, business services |
| **Transient** | Created each time | Validators, parsers, DTOs |

### 10. Configuration File Example

**appsettings.json**:
```json
{
  "Database": {
    "ConnectionString": "Data Source={AppData}\\DocMind\\documents.db",
    "CacheRetentionDays": 30,
    "MaxConnections": 10
  },
  "Ai": {
    "DefaultProvider": "Local",
    "LocalModelPath": "Models\\Phi-3-mini-4k-instruct-q4.gguf",
    "ContextSize": 2048,
    "UseGpu": false,
    "Temperature": 0.2,
    "MaxTokens": 512
  },
  "Ui": {
    "Theme": "Dark",
    "Language": "ru-RU",
    "AutoSaveInterval": 300,
    "RecentDocumentsLimit": 10
  },
  "Logging": {
    "Level": "Information",
    "File": "Logs\\docmind-.log",
    "RollingInterval": "Day",
    "RetainedFileCountLimit": 7
  }
}
```

### 11. Environment-Specific Configuration

**appsettings.Development.json**:
```json
{
  "Ai": {
    "ContextSize": 1024,
    "UseGpu": true
  },
  "Logging": {
    "Level": "Debug"
  }
}
```

**appsettings.Production.json**:
```json
{
  "Ai": {
    "ContextSize": 2048,
    "UseGpu": false
  },
  "Logging": {
    "Level": "Warning"
  }
}
```

## Benefits of This DI Configuration

1. **Separation of Concerns**: Each layer has its own DI configuration
2. **Testability**: Easy to mock dependencies for unit testing
3. **Flexibility**: Environment-specific configurations
4. **Maintainability**: Clear service registration patterns
5. **Performance**: Appropriate service lifetimes
6. **Scalability**: Easy to add new services and features