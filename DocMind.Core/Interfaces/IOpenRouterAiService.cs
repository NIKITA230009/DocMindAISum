using System.Threading;
using System.Threading.Tasks;

namespace DocMind.Core.Interfaces;

public interface IOpenRouterAiService
{
    bool IsConfigured { get; }
    void SetApiKey(string apiKey);
    Task<string> ExecuteCommandAsync(string text, string command, CancellationToken ct = default);
}
