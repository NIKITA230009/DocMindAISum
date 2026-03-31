using DocMind.Core.Interfaces;
using LiteDB;
using Microsoft.Extensions.Logging;

namespace DocMind.Data.Repositories.Implementations;

public abstract class BaseRepository<TEntity, TKey> : IRepository<TEntity, TKey> 
    where TEntity : class
{
    protected readonly ILiteDatabaseContext _context;
    protected readonly string _collectionName;
    protected readonly ILogger<BaseRepository<TEntity, TKey>> _logger;
    
    protected BaseRepository(
        ILiteDatabaseContext context,
        string collectionName,
        ILogger<BaseRepository<TEntity, TKey>> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _collectionName = collectionName ?? throw new ArgumentNullException(nameof(collectionName));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
    
    public virtual async Task<IEnumerable<TEntity>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var collection = GetCollection();
            return collection.Query()
                .Skip(page * pageSize)
                .Limit(pageSize)
                .ToList();
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
    
    public virtual async Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var collection = GetCollection();
            return collection.FindById(new BsonValue(id)) != null;
        }, cancellationToken);
    }
    
    public virtual async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var collection = GetCollection();
            return collection.Count();
        }, cancellationToken);
    }
}