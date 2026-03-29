using System;
using System.Threading;
using System.Threading.Tasks;
using DocMind.Core.Dto;
using DocMind.Core.Interfaces;

namespace DocMind.Services.Services;

public class AiService : IAiService
{
    private readonly IAiApi _aiApi;

    public AiService(IAiApi aiApi)
    {
        _aiApi = aiApi;
    }

    public async Task<string> SummarizeAsync(string text, CancellationToken ct = default)
    {
        try
        {
            var request = new SummarizeRequest { Text = text };
            var response = await _aiApi.SummarizeAsync(request);
            return response.Result;
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка при обращении к AI-серверу: {ex.Message}", ex);
        }
    }
    public async Task<string> ExecuteCommandAsync(string text, string command, CancellationToken ct = default)
    {
        var request = new ExecuteCommandRequest { Text = text, Command = command };
        var response = await _aiApi.ExecuteCommandAsync(request);
        return response.Result;
    }
}