using DocMind.Core.Dto;
using Refit;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DocMind.Core.Interfaces;

public interface IAiApi
{
    [Post("/api/ai/summarize")]
    Task<SummarizeResponse> SummarizeAsync(SummarizeRequest request);

    [Post("/api/ai/execute")]
    Task<ExecuteCommandResponse> ExecuteCommandAsync(ExecuteCommandRequest request);
}