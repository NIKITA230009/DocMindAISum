using System.Collections.Generic;
using System.Threading.Tasks;
using DocMind.Core.Models;

namespace DocMind.Core.Interfaces;

public interface IQueryHistoryService
{
    Task AddAsync(QueryHistory entry);
    Task<IEnumerable<QueryHistory>> GetRecentAsync(int limit = 20);
    Task ClearAsync();
}