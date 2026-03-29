using System.Threading;
using System.Threading.Tasks;

namespace DocMind.Core.Interfaces;

public interface IAiService
{
    Task<string> SummarizeAsync(string text, CancellationToken ct = default);
    Task<string> ExecuteCommandAsync(string text, string command, CancellationToken ct);
}