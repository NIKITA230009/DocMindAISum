# Repository Pattern Implementation Plan

## Current Data Access Analysis

**Current Implementation Issues**:
1. Direct LiteDB usage in services (`LiteDbDocumentCache.cs`)
2. No abstraction layer for data access
3. Mixed concerns (caching logic with data access)
4. Hardcoded database paths and connection strings
5. No unit of work pattern for transactions
6. Limited query capabilities

## Proposed Repository Pattern Architecture

### 1. Repository Layer Structure

```
DocMind.Data/
├── Contexts/
│   └── LiteDatabaseContext.cs      # Database context wrapper
├── Entities/
│   ├── DocumentEntity.cs           # Database entity for documents
│   ├── QueryHistoryEntity.cs       # Database entity for query history
│   └── CacheEntity.cs              # Database entity for cache
├── Repositories/
│   ├── Interfaces/
│   │   ├── IRepository.cs          # Generic repository interface
│   │   ├── IDocumentRepository.cs  # Document-specific repository
│   │   ├── IQueryHistoryRepository.cs
│   │   └── ICacheRepository.cs
│   └── Implementations/
│       ├── BaseRepository.cs       # Generic repository implementation
│       ├── DocumentRepository.cs
│       ├── QueryHistoryRepository.cs
│       └── CacheRepository.cs
├── UnitOfWork/
│   ├── IUnitOfWork.cs              # Unit of work interface
│   └── UnitOfWork.cs               # Unit of work implementation
└── Migrations/
    └── DatabaseMigrator.cs         # Database migration utility
```

### 2. Core Repository Interfaces

**Generic Repository Interface**:
```csharp
namespace DocMind.Data.Repositories.Interfaces;

public interface IRepository<TEntity, TKey> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<TEntity>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(TKey id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
```

**Document-Specific Repository Interface**:
```csharp
namespace DocMind.Data.Repositories.Interfaces;

public interface IDocumentRepository : IRepository<DocumentEntity, string>
{
    Task<DocumentEntity?> GetByPathAsync(string filePath, CancellationToken cancellationToken = default);
    Task<IEnumerable<DocumentEntity>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
    Task<IEnumerable<DocumentEntity>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task CleanupOldEntriesAsync(TimeSpan maxAge, CancellationToken cancellationToken = default);
    Task UpdateLastOpenedAsync(string filePath, DateTime lastOpened, CancellationToken cancellationToken = default);
}
```

### 3. Base Repository Implementation

**BaseRepository.cs**:
```csharp
using LiteDB;
using DocMind.Data.Contexts;
using DocMind.Data.Repositories.Interfaces;

namespace DocMind.Data.Repositories.Implementations;

public abstract class BaseRepository<TEntity, TKey> : IRepository<TEntity, TKey> 
    where TEntity : class
{
    protected readonly ILiteDatabaseContext _context;
    protected readonly string _collectionName;
    
    protected BaseRepository(ILiteDatabaseContext context, string collectionName)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _collectionName = collectionName ?? throw new ArgumentNullException(nameof(collectionName));
    }
    
    protected ILiteCollection<TEntity> GetCollection()
    {
        return _context.Database.GetCollection<TEntity>(_collectionName);
    }
    
    public virtual async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var collection = GetCollection();
            return collection.FindById(new BsonValue(id));
        }, cancellationToken);
    }
    
    public virtual async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var collection = GetCollection();
            return collection.FindAll().ToList();
        }, cancellationToken);
    }
    
    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            var collection = GetCollection();
            collection.Insert(entity);
        }, cancellationToken);
    }
    
    public virtual async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            var collection = GetCollection();
            collection.Update(entity);
        }, cancellationToken);
    }
    
    public virtual async Task DeleteAsync(TKey id, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            var collection = GetCollection();
            collection.Delete(new BsonValue(id));
        }, cancellationToken);
    }
    
    // Additional methods implementation...
}
```

### 4. Document Repository Implementation

**DocumentRepository.cs**:
```csharp
using DocMind.Data.Contexts;
using DocMind.Data.Entities;
using DocMind.Data.Repositories.Interfaces;
using LiteDB;
using Microsoft.Extensions.Logging;

namespace DocMind.Data.Repositories.Implementations;

public class DocumentRepository : BaseRepository<DocumentEntity, string>, IDocumentRepository
{
    private readonly ILogger<DocumentRepository> _logger;
    
    public DocumentRepository(
        ILiteDatabaseContext context,
        ILogger<DocumentRepository> logger)
        : base(context, "documents")
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<DocumentEntity?> GetByPathAsync(string filePath, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting document by path: {FilePath}", filePath);
        
        return await Task.Run(() =>
        {
            var collection = GetCollection();
            return collection.FindOne(x => x.FilePath == filePath);
        }, cancellationToken);
    }
    
    public async Task<IEnumerable<DocumentEntity>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting {Count} recent documents", count);
        
        return await Task.Run(() =>
        {
            var collection = GetCollection();
            return collection.Query()
                .OrderByDescending(x => x.LastOpened)
                .Limit(count)
                .ToList();
        }, cancellationToken);
    }
    
    public async Task<IEnumerable<DocumentEntity>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Searching documents with term: {SearchTerm}", searchTerm);
        
        return await Task.Run(() =>
        {
            var collection = GetCollection();
            return collection.Query()
                .Where(x => x.Content.Contains(searchTerm) || 
                           x.FilePath.Contains(searchTerm) ||
                           x.Title.Contains(searchTerm))
                .ToList();
        }, cancellationToken);
    }
    
    public async Task CleanupOldEntriesAsync(TimeSpan maxAge, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cleaning up documents older than {MaxAge}", maxAge);
        
        await Task.Run(() =>
        {
            var collection = GetCollection();
            var cutoff = DateTime.UtcNow - maxAge;
            var deleted = collection.DeleteMany(x => x.LastOpened < cutoff);
            
            _logger.LogInformation("Deleted {Count} old documents", deleted);
        }, cancellationToken);
    }
    
    public async Task UpdateLastOpenedAsync(string filePath, DateTime lastOpened, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating last opened for document: {FilePath}", filePath);
        
        await Task.Run(() =>
        {
            var collection = GetCollection();
            var document = collection.FindOne(x => x.FilePath == filePath);
            if (document != null)
            {
                document.LastOpened = lastOpened;
                collection.Update(document);
            }
        }, cancellationToken);
    }
}
```

### 5. Database Context Abstraction

**ILiteDatabaseContext.cs**:
```csharp
namespace DocMind.Data.Contexts;

public interface ILiteDatabaseContext : IDisposable
{
    ILiteDatabase Database { get; }
    Task EnsureIndexesAsync(CancellationToken cancellationToken = default);
    Task MigrateAsync(CancellationToken cancellationToken = default);
}
```

**LiteDatabaseContext.cs**:
```csharp
using LiteDB;
using Microsoft.Extensions.Options;
using DocMind.Infrastructure.Configuration;

namespace DocMind.Data.Contexts;

public class LiteDatabaseContext : ILiteDatabaseContext
{
    private readonly LiteDatabase _database;
    private readonly DatabaseSettings _settings;
    
    public LiteDatabaseContext(IOptions<DatabaseSettings> settings)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        
        var connectionString = new ConnectionString(_settings.ConnectionString)
        {
            Connection = ConnectionType.Shared
        };
        
        _database = new LiteDatabase(connectionString);
    }
    
    public ILiteDatabase Database => _database;
    
    public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            // Create indexes for better query performance
            var documents = _database.GetCollection<DocumentEntity>("documents");
            documents.EnsureIndex(x => x.FilePath, unique: true);
            documents.EnsureIndex(x => x.LastOpened);
            documents.EnsureIndex(x => x.Hash);
            
            var history = _database.GetCollection<QueryHistoryEntity>("query_history");
            history.EnsureIndex(x => x.Timestamp);
            history.EnsureIndex(x => x.DocumentId);
            
            var cache = _database.GetCollection<CacheEntity>("cache");
            cache.EnsureIndex(x => x.Key, unique: true);
            cache.EnsureIndex(x => x.ExpiresAt);
        }, cancellationToken);
    }
    
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            // Run database migrations
            var migrator = new DatabaseMigrator(_database);
            migrator.Migrate();
        }, cancellationToken);
    }
    
    public void Dispose()
    {
        _database?.Dispose();
        GC.SuppressFinalize(this);
    }
}
```

### 6. Unit of Work Pattern

**IUnitOfWork.cs**:
```csharp
namespace DocMind.Data.UnitOfWork;

public interface IUnitOfWork : IDisposable
{
    IDocumentRepository Documents { get; }
    IQueryHistoryRepository QueryHistory { get; }
    ICacheRepository Cache { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
```

**UnitOfWork.cs**:
```csharp
using DocMind.Data.Contexts;
using DocMind.Data.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace DocMind.Data.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly ILiteDatabaseContext _context;
    private readonly ILogger<UnitOfWork> _logger;
    private IDocumentRepository? _documentRepository;
    private IQueryHistoryRepository? _queryHistoryRepository;
    private ICacheRepository? _cacheRepository;
    
    public UnitOfWork(
        ILiteDatabaseContext context,
        ILogger<UnitOfWork> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public IDocumentRepository Documents => 
        _documentRepository ??= new DocumentRepository(_context, _logger);
    
    public IQueryHistoryRepository QueryHistory => 
        _queryHistoryRepository ??= new QueryHistoryRepository(_context, _logger);
    
    public ICacheRepository Cache => 
        _cacheRepository ??= new CacheRepository(_context, _logger);
    
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // LiteDB auto-commits, but we can implement batching here
        _logger.LogDebug("Unit of work saved changes");
        return Task.FromResult(0);
    }
    
    public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        // LiteDB doesn't support transactions in the same way as SQL databases
        // We can implement a transaction log or use a different approach
        _logger.LogDebug("Beginning transaction");
        return Task.CompletedTask;
    }
    
    public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Committing transaction");
        return Task.CompletedTask;
    }
    
    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Rolling back transaction");
        return Task.CompletedTask;
    }
    
    public void Dispose()
    {
        _context?.Dispose();
        GC.SuppressFinalize(this);
    }
}
```

### 7. Entity Definitions

**DocumentEntity.cs**:
```csharp
using LiteDB;

namespace DocMind.Data.Entities;

public class DocumentEntity
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    
    [BsonField("file_path")]
    public string FilePath { get; set; } = string.Empty;
    
    [BsonField("title")]
    public string Title { get; set; } = string.Empty;
    
    [BsonField("content")]
    public string Content { get; set; } = string.Empty;
    
    [BsonField("hash")]
    public string Hash { get; set; } = string.Empty;
    
    [BsonField("last_opened")]
    public DateTime LastOpened { get; set; } = DateTime.UtcNow;
    
    [BsonField("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [BsonField("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new();
    
    [BsonField("size")]
    public long Size { get; set; }
    
    [BsonField("format")]
    public string Format { get; set; } = string.Empty;
}
```

### 8. Migration from Current Implementation

**Migration Strategy**:

1. **Phase 1**: Create new repository interfaces and base implementation
2. **Phase 2**: Update service layer to use repositories instead of direct LiteDB
3. **Phase 3**: Migrate existing data to new entity structure
4. **Phase 4**: Update DI configuration to use new repositories
5. **Phase 5**: Remove old data access code

**Step-by-Step Migration**:

```csharp
// Current service using direct LiteDB
public class OldDocumentCacheService : IDocumentCacheService
{
    // Direct LiteDB usage
}

// New service using repository pattern
public class NewDocumentCacheService : IDocumentCacheService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly ILogger<NewDocumentCacheService> _logger;
    
    public NewDocumentCacheService(
        IDocumentRepository documentRepository,
        ILogger<NewDocumentCacheService> logger)
    {
        _documentRepository = documentRepository;
        _logger = logger;
    }
    
    public async Task AddOrUpdateAsync(StoredDocument document)
    {
        // Convert to entity and use repository
        var entity = MapToEntity(document);
        await _documentRepository.AddAsync(entity);
    }
    
    // Other methods...
}
```

### 9. Benefits of Repository Pattern

1. **Abstraction**: Hide database implementation details from business logic
2. **Testability**: Easy to mock repositories for unit testing
3. **Maintainability**: Centralized data access logic
4. **Flexibility**: Easy to switch database providers
5. **Consistency**: Standardized data access patterns
6. **Performance**: Optimized queries and indexing
7. **Security**: Centralized data validation and sanitization

### 10. Implementation Timeline

| Phase | Task | Estimated Complexity |
|-------|------|---------------------|
| 1 | Create repository interfaces and base classes | Low |
| 2 | Implement entity classes with LiteDB attributes | Low |
| 3 | Create database context and unit of work | Medium |
| 4 | Implement specific repositories | Medium |
| 5 | Update service layer to use repositories | High |
| 6 | Update DI configuration | Low |
| 7 | Data migration and testing | High |
| 8 | Performance optimization | Medium |

### 11. Testing Strategy

**Unit Tests**:
- Test repository methods in isolation
- Mock LiteDB context
- Test entity mapping

**Integration Tests**:
- Test actual database operations
- Test transaction handling
- Test performance with large datasets

**Migration Tests**:
- Test data migration from old to new structure
- Verify data integrity
- Test backward compatibility