using DocMind.Core.Interfaces;
using LLama;
using LLama.Common;
using LLama.Sampling;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DocMind.Services.Services;

public class LLamaSharpAiService : ILocalAiService, IDisposable
{
    private readonly LLamaWeights _weights;
    private readonly ModelParams _modelParams;
    private readonly InferenceParams _inferenceParams;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    // Новый системный промпт — только суммаризация и перефразирование
    private const string SystemPrompt =
        "You are DocMind AI, a helpful assistant for text summarization and paraphrasing.\n" +
        "You receive a document text and a user command. The command will be either 'summarize' or 'paraphrase'.\n" +
        "You must output ONLY the result, without any additional explanations, comments, or the original text.\n\n" +
        "Examples:\n" +
        "Document: \"The meeting was very long and we discussed many things like budget and hiring.\"\n" +
        "Command: summarize\n" +
        "Output: \"Meeting covered budget and hiring.\"\n\n" +
        "Document: \"He go to school yesterday.\"\n" +
        "Command: paraphrase\n" +
        "Output: \"He went to school yesterday.\"\n\n" +
        "Document: \"Солнце светит ярко, птицы поют.\"\n" +
        "Command: summarize\n" +
        "Output: \"Солнечная погода, птицы поют.\"\n\n" +
        "Document: \"Please send the report by Friday. Also send the invoice.\"\n" +
        "Command: paraphrase\n" +
        "Output: \"Kindly submit the report and the invoice by Friday.\"\n\n" +
        "Now process the following request. Remember: output ONLY the result.";

    public LLamaSharpAiService()
    {
        string modelPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Phi-3-mini-4k-instruct-q4.gguf");

        if (!File.Exists(modelPath))
    {
        // можно установить флаг, что модель недоступна, и возвращать заглушку в ExecuteCommandAsync
        return;
    }

        _modelParams = new ModelParams(modelPath)
        {
            ContextSize = 2048,          // уменьшили для скорости
            GpuLayerCount = 0,
            BatchSize = 1024,
            Threads = Environment.ProcessorCount
        };

        _weights = LLamaWeights.LoadFromFile(_modelParams);

        var pipeline = new DefaultSamplingPipeline
        {
            Temperature = 0.2f,
            TopP = 0.85f,
            TopK = 30,
            RepeatPenalty = 1.4f,
            PresencePenalty = 0.2f,
            FrequencyPenalty = 0.2f
        };

        _inferenceParams = new InferenceParams
        {
            MaxTokens = 512,            // для короткого ответа достаточно
            SamplingPipeline = pipeline,
            AntiPrompts = new List<string> { "User:", "Document:", "Output:" }
        };
    }

    public async Task<string> ExecuteCommandAsync(string text, string command, CancellationToken ct = default)
    {
        // summarize или paraphrase
        string normalizedCommand = command.Trim().ToLowerInvariant();
        string action = normalizedCommand.Contains("суммар") || normalizedCommand.Contains("summar") ? "summarize"
                     : normalizedCommand.Contains("перефраз") || normalizedCommand.Contains("paraphr") ? "paraphrase"
                     : "summarize"; // по умолчанию суммаризация

        // Формируем сообщение пользователя
        string userMessage = $"Document: {text}\nCommand: {action}\nOutput:";

        await _semaphore.WaitAsync(ct);
        try
        {
            using var context = _weights.CreateContext(_modelParams);
            var executor = new InteractiveExecutor(context);

            var chatHistory = new ChatHistory();
            chatHistory.AddMessage(AuthorRole.System, SystemPrompt);

            var session = new ChatSession(executor, chatHistory);
            var response = new StringBuilder();

            await foreach (var token in session.ChatAsync(
                new ChatHistory.Message(AuthorRole.User, userMessage),
                _inferenceParams, ct))
            {
                response.Append(token);
            }

            string result = response.ToString().Trim();

            // Удаляем возможные префиксы
            string[] prefixes = { "Output:", "Result:", "Assistant:", "**response:**" };
            foreach (var prefix in prefixes)
            {
                if (result.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    result = result.Substring(prefix.Length).TrimStart();
                }
            }

            result = result.Replace("User:", "");

            // Если результат пуст или содержит только пробелы, возвращаем сообщение об ошибке
            if (string.IsNullOrWhiteSpace(result))
                return "Не удалось обработать запрос. Попробуйте ещё раз.";

            return result;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        _weights?.Dispose();
        _semaphore?.Dispose();
    }
}