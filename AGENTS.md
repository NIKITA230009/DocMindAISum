# DocMind Agent Instructions

This document provides guidelines for AI agents working in the DocMind codebase.

## Project Overview

DocMind is a desktop AI application for working with Word documents, built with C#/.NET 10 and Avalonia UI 11.3.

### Architecture

```
DocMind.Core/           - Domain models, interfaces, DTOs
DocMind.Data/           - Repositories, database contexts, entities
DocMind.Infrastructure/ - Configuration, logging, DI setup
DocMind.Services/       - Business logic, AI services
DocMind.Desktop/        - Avalonia UI, ViewModels (MVVM)
DocMind.Tests/          - xUnit unit tests
```

## Build Commands

### Build the entire solution
```powershell
dotnet build
```

### Build a specific project
```powershell
dotnet build DocMind.Services/DocMind.Services.csproj
```

### Release build
```powershell
dotnet build -c Release
```

### Clean and rebuild
```powershell
dotnet clean; dotnet build
```

## Test Commands

### Run all tests
```powershell
dotnet test
```

### Run tests for a specific project
```powershell
dotnet test DocMind.Tests/DocMind.Tests.csproj
```

### Run a single test class
```powershell
dotnet test --filter "FullyQualifiedName~UnitTest1"
```

### Run a single test method
```powershell
dotnet test --filter "FullyQualifiedName~UnitTest1.Test1"
```

### Run tests with verbose output
```powershell
dotnet test -v n
```

### Run tests with coverage
```powershell
dotnet test --collect:"XPlat Code Coverage"
```

## Code Style Guidelines

### Naming Conventions

- **Classes/Methods/Properties**: PascalCase
  ```csharp
  public class DocumentService { }
  public string FilePath { get; set; }
  Task<string> SummarizeAsync(string text);
  ```

- **Interfaces**: Prefix with `I`
  ```csharp
  public interface IAiService { }
  public interface IDocumentRepository { }
  ```

- **Private fields**: `_camelCase` (prefixed with underscore)
  ```csharp
  private readonly IAiApi _aiApi;
  private readonly ILogger<ConfigurationService> _logger;
  ```

- **Parameters**: camelCase
  ```csharp
  public async Task<string> SummarizeAsync(string text, CancellationToken ct = default)
  ```

- **Files**: Match the class name (e.g., `DocumentService.cs`)

### Async Patterns

- All async methods must have `Async` suffix
- Always accept `CancellationToken` as optional parameter (last position)
- Use `ConfigureAwait(false)` when appropriate
  ```csharp
  Task<string> SummarizeAsync(string text, CancellationToken ct = default);
  ```

### Nullable Reference Types

- Enable `<Nullable>enable</Nullable>` in all projects
- Use nullable annotations: `string?`, `MyClass?`
- Initialize with `string.Empty` or `null` as appropriate
  ```csharp
  public string FilePath { get; set; } = string.Empty;
  private StoredDocument? _selectedDocument;
  ```

### Dependency Injection

- Use constructor injection for all dependencies
- Guard against null with `ArgumentNullException`
  ```csharp
  public AiService(IAiApi aiApi)
  {
      _aiApi = aiApi ?? throw new ArgumentNullException(nameof(aiApi));
  }
  ```

- Register services in `ServiceCollectionExtensions.cs`
- Use `Microsoft.Extensions.DependencyInjection`

### Error Handling

- Use try-catch with logging
- Re-throw with context using `throw new Exception("message", ex)`
  ```csharp
  try {
      var response = await _aiApi.SummarizeAsync(request);
      return response.Result;
  }
  catch (Exception ex) {
      throw new Exception($"Ошибка при обращении к AI-серверу: {ex.Message}", ex);
  }
  ```

### Logging

- Use `Microsoft.Extensions.Logging`
- Inject `ILogger<T>` into services
- Log levels: `LogDebug`, `LogInformation`, `LogWarning`, `LogError`
  ```csharp
  _logger.LogDebug("Getting document by path: {FilePath}", filePath);
  _logger.LogError(ex, "Failed to load configuration");
  ```

### Project References

Follow layer dependencies:
```
Desktop → Services → (Data, Infrastructure) → Core
Data → (Infrastructure, Core)
Infrastructure → Core
```

### Interfaces Location

- All interfaces belong in `DocMind.Core/Interfaces/`
- Name: `I<ServiceName>.cs` (e.g., `IAiService.cs`)

### Models/DTOs Location

- Domain models: `DocMind.Core/Models/`
- DTOs: `DocMind.Core/Dto/`
- Database entities: `DocMind.Data/Entities/`

### Repository Pattern

- Implement `IRepository<TEntity, TKey>` in `BaseRepository`
- Specific repositories inherit from `BaseRepository`
- Use `ILiteDatabaseContext` abstraction for database access

### MVVM Pattern (Avalonia)

- ViewModels inherit from `ObservableObject` (CommunityToolkit.Mvvm)
- Use `[ObservableProperty]` for observable properties
- Use `[RelayCommand]` for commands
- Use `[NotifyPropertyChangedFor]` for dependent properties
  ```csharp
  [ObservableProperty]
  private string _documentContent = string.Empty;
  
  [RelayCommand]
  private async Task OpenDocument() { }
  ```

### Configuration

- Use `Microsoft.Extensions.Configuration`
- Settings models in `DocMind.Infrastructure/Configuration/Models/`
- Use `IOptions<T>` pattern for strongly-typed settings
- Use FluentValidation for settings validation

### Serialization Attributes (LiteDB)

- Use `[BsonId]` for primary key
- Use `[BsonField("name")]` for custom field names
  ```csharp
  [BsonId]
  public string Id { get; set; } = string.Empty;
  
  [BsonField("file_path")]
  public string FilePath { get; set; } = string.Empty;
  ```

### Unit Test Patterns

- Use xUnit with `[Fact]` attribute
- Use `Theory` with `[InlineData]` for parameterized tests
- Mock dependencies using Moq (if added)
- Place tests in `DocMind.Tests/`
- Test file naming: `<ClassName>Tests.cs`

## Technology Stack

| Component | Technology |
|-----------|------------|
| UI Framework | Avalonia UI 11.3 |
| MVVM | CommunityToolkit.Mvvm 8.2 |
| DI | Microsoft.Extensions.DI 10.0 |
| Database | LiteDB 5.0 |
| AI | LLamaSharp 0.26 |
| Document | OpenXML SDK 3.4 |
| Logging | Serilog 3.1 |
| Configuration | Microsoft.Extensions.Configuration 10.0 |
| Validation | FluentValidation 11.10 |
| Testing | xUnit 2.9 |

## Common Tasks

### Add a new service
1. Create interface in `DocMind.Core/Interfaces/I<Name>Service.cs`
2. Implement in `DocMind.Services/Services/<Name>Service.cs`
3. Register in `ServiceCollectionExtensions.cs`
4. Inject into consumers via constructor

### Add a new model
1. Domain model: `DocMind.Core/Models/<Name>Model.cs`
2. DTO: `DocMind.Core/Dto/<Name>Request.cs` and `<Name>Response.cs`
3. Entity: `DocMind.Data/Entities/<Name>Entity.cs`

### Add a new repository
1. Define interface in `DocMind.Core/Interfaces/I<Name>Repository.cs`
2. Implement in `DocMind.Data/Repositories/Implementations/<Name>Repository.cs`
3. Inherit from `BaseRepository<TEntity, TKey>`
4. Register in DI container
