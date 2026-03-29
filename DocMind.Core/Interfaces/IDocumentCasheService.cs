using DocMind.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DocMind.Core.Interfaces
{
    public interface IDocumentCacheService
    {
        Task AddOrUpdateAsync(StoredDocument document);
        Task<StoredDocument?> GetByPathAsync(string filePath);
        Task<List<StoredDocument>> GetRecentAsync(int count = 10);
        Task RemoveAsync(string filePath);
        Task CleanupOldEntriesAsync(TimeSpan maxAge);
    }
}