using LiteDB;

namespace DocMind.Core.Interfaces;

public interface ILiteDatabaseContext : IDisposable
{
    ILiteDatabase Database { get; }
    Task EnsureIndexesAsync(CancellationToken cancellationToken = default);
    Task MigrateAsync(CancellationToken cancellationToken = default);
}