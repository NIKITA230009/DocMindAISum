using System.Threading;
using System.Threading.Tasks;

namespace DocMind.Core.Interfaces;

public interface ILocalAiService
{
    Task<string> ExecuteCommandAsync(string text, string command, CancellationToken ct = default);
}