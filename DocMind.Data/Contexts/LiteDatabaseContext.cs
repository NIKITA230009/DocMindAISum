using DocMind.Core.Interfaces;
using LiteDB;
using Microsoft.Extensions.Options;
using DocMind.Infrastructure.Configuration.Models;

namespace DocMind.Data.Contexts;

public class LiteDatabaseContext : ILiteDatabaseContext
{
    private readonly LiteDatabase _database;
    private readonly DatabaseSettings _settings;
    
    public LiteDatabaseContext(IOptions<DatabaseSettings> settings)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        
        var connectionString = new ConnectionString(_settings.ResolvedConnectionString)
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
            var documents = _database.GetCollection<DocMind.Core.Models.StoredDocument>("documents");
            documents.EnsureIndex(x => x.FilePath, unique: true);
            
            // Additional indexes will be added here
        }, cancellationToken);
    }
    
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            // Run database migrations if needed
            // Currently LiteDB doesn't need migrations for schema changes
        }, cancellationToken);
    }
    
    public void Dispose()
    {
        _database?.Dispose();
        GC.SuppressFinalize(this);
    }
}