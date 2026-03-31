# DocMind Architecture Design

## Current Architecture Assessment

The existing DocMind application has a solid foundation with:
- Clear project separation (Core, Services, Desktop, Tests)
- Dependency injection with Microsoft.Extensions.DependencyInjection
- MVVM pattern using CommunityToolkit.Mvvm
- Local AI integration with LLamaSharp
- LiteDB for document caching

## Proposed Architectural Improvements

### 1. Enhanced Modular Structure

```
DocMind.sln
├── DocMind.Core/              # Core abstractions and interfaces
│   ├── Interfaces/           # Service contracts
│   ├── Models/              # Domain models and DTOs
│   ├── Enums/               # Application enums
│   └── Constants/           # Application constants
├── DocMind.Data/            # Data access layer
│   ├── Repositories/        # Repository implementations
│   ├── Entities/            # Database entities
│   ├── Contexts/           # LiteDB context
│   └── Migrations/         # Database migrations
├── DocMind.Services/        # Business logic layer
│   ├── Document/           # Document processing services
│   ├── AI/                 # AI services (LLamaSharp, OpenAI, etc.)
│   ├── Cache/              # Caching services
│   └── History/            # Query history services
├── DocMind.Infrastructure/  # Infrastructure services
│   ├── Configuration/      # App configuration
│   ├── Logging/           # Structured logging
│   ├── FileSystem/        # File operations
│   └── External/          # External API clients
├── DocMind.Desktop/        # Presentation layer (Avalonia UI)
│   ├── Views/             # Avalonia views
│   ├── ViewModels/        # MVVM view models
│   ├── Converters/        # Value converters
│   └── Behaviors/         # UI behaviors
└── DocMind.Tests/          # Test projects
    ├── UnitTests/         # Unit tests
    ├── IntegrationTests/  # Integration tests
    └── UITests/          # UI tests
```

### 2. Dependency Injection Configuration

**Current Issue**: DI configuration mixed in App.axaml.cs
**Solution**: Create dedicated DI configuration classes

```csharp
// DocMind.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services)
    {
        services.AddSingleton<ILiteDatabaseContext, LiteDatabaseContext>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IQueryHistoryRepository, QueryHistoryRepository>();
        return services;
    }

    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IAiService, AiService>();
        services.AddScoped<IDocumentCacheService, DocumentCacheService>();
        services.AddScoped<IQueryHistoryService, QueryHistoryService>();
        return services;
    }

    public static IServiceCollection AddAiServices(this IServiceCollection services, 
        Action<AiServiceOptions> configureOptions)
    {
        services.Configure(configureOptions);
        services.AddSingleton<ILocalAiService, LLamaSharpAiService>();
        services.AddSingleton<IAiServiceFactory, AiServiceFactory>();
        return services;
    }
}
```

### 3. Repository Pattern Implementation

**Current Issue**: Direct LiteDB usage in services
**Solution**: Abstract data access with repository pattern

```csharp
// Core interface
public interface IRepository<TEntity, TKey>
{
    Task<TEntity?> GetByIdAsync(TKey id);
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task AddAsync(TEntity entity);
    Task UpdateAsync(TEntity entity);
    Task DeleteAsync(TKey id);
}

// Document repository specialization
public interface IDocumentRepository : IRepository<StoredDocument, string>
{
    Task<IEnumerable<StoredDocument>> GetRecentAsync(int count);
    Task<StoredDocument?> GetByPathAsync(string filePath);
    Task CleanupOldEntriesAsync(TimeSpan maxAge);
}
```

### 4. Configuration Management

**Current Issue**: Hardcoded paths and settings
**Solution**: Centralized configuration system

```csharp
public class AppSettings
{
    public DatabaseSettings Database { get; set; } = new();
    public AiSettings Ai { get; set; } = new();
    public UiSettings Ui { get; set; } = new();
}

public class DatabaseSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public int CacheRetentionDays { get; set; } = 30;
}

public class AiSettings
{
    public string ModelPath { get; set; } = string.Empty;
    public string DefaultModel { get; set; } = "Phi-3-mini-4k-instruct-q4.gguf";
    public int ContextSize { get; set; } = 2048;
    public bool UseGpu { get; set; } = false;
}
```

### 5. Enhanced Service Layer Design

**Document Service Improvements**:
- Separate file operations from business logic
- Add validation and error handling
- Support multiple document formats (DOCX, PDF, TXT)

**AI Service Improvements**:
- Strategy pattern for multiple AI providers
- Fallback mechanisms
- Request/response pipeline with middleware
- Prompt templating system

**Caching Service Improvements**:
- Multi-level caching (memory, disk, distributed)
- Cache invalidation strategies
- Performance monitoring

### 6. Error Handling and Logging

**Structured Logging**:
- Use Serilog with structured logging
- Correlation IDs for request tracing
- Log enrichment with context

**Global Error Handling**:
- Exception middleware
- User-friendly error messages
- Error recovery strategies

### 7. Testing Strategy

**Unit Tests**:
- Mock dependencies using Moq or NSubstitute
- Test service logic in isolation
- Cover edge cases and error scenarios

**Integration Tests**:
- Test database interactions
- Test file system operations
- Test AI service integration

**UI Tests**:
- Avalonia UI testing framework
- End-to-end user workflows
- Visual regression testing

## Implementation Priority

1. **Phase 1**: Repository pattern and configuration management
2. **Phase 2**: Enhanced service layer with proper abstractions
3. **Phase 3**: Improved error handling and logging
4. **Phase 4**: Testing infrastructure
5. **Phase 5**: Additional features and optimizations

## Benefits of This Architecture

1. **Better Separation of Concerns**: Each layer has clear responsibilities
2. **Improved Testability**: Mockable interfaces and dependency injection
3. **Enhanced Maintainability**: Clear boundaries between components
4. **Scalability**: Easy to add new features and services
5. **Flexibility**: Support for multiple AI providers and document formats
6. **Robustness**: Comprehensive error handling and logging