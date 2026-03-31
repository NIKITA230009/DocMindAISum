using DocMind.Core.Models;

namespace DocMind.Core.Interfaces;

public interface IDocumentRepository : IRepository<StoredDocument, string>
{
    Task<StoredDocument?> GetByPathAsync(string filePath, CancellationToken cancellationToken = default);
    Task<IEnumerable<StoredDocument>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
    Task<IEnumerable<StoredDocument>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task CleanupOldEntriesAsync(TimeSpan maxAge, CancellationToken cancellationToken = default);
    Task UpdateLastOpenedAsync(string filePath, DateTime lastOpened, CancellationToken cancellationToken = default);
}