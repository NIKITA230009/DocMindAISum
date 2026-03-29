using DocMind.Core.Interfaces;
using DocMind.Core.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DocMind.Services.Services;

public class WordDocumentService : IDocumentService
{
    private readonly IDocumentCacheService _cacheService;

    public WordDocumentService(IDocumentCacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public Task<DocumentModel> LoadDocumentAsync(string filePath)
    {
        return Task.Run(async () =>
        {


            Debug.WriteLine($"[СЕРВИС] LoadDocumentAsync: путь = {filePath}");
            var content = string.Empty;
            using (var wordDoc = WordprocessingDocument.Open(filePath, false))
            {
                var body = wordDoc.MainDocumentPart?.Document?.Body;
                if (body != null)
                {
                    var paragraphs = body.Elements<Paragraph>().ToList();
                    Debug.WriteLine($"[СЕРВИС] Найдено параграфов: {paragraphs.Count}");
                    content = string.Join(Environment.NewLine, paragraphs.Select(p => p.InnerText));
                    Debug.WriteLine($"[СЕРВИС] Длина извлечённого текста: {content.Length}");
                }
                else
                {
                    Debug.WriteLine("[СЕРВИС] Body = null");
                }
            }
            // Вычисляем хеш содержимого
            var hash = LiteDbDocumentCache.ComputeHash(content);

            // Проверяем, есть ли уже в кэше запись с таким же путём и хешем
            var cached = await _cacheService.GetByPathAsync(filePath);
            if (cached != null && cached.Hash == hash)
            {
                // Можно загрузить из кэша (но мы уже прочитали файл, поэтому просто обновим дату)
                cached.LastOpened = DateTime.UtcNow;
                await _cacheService.AddOrUpdateAsync(cached);
            }
            else
            {
                // Создаём новую запись в кэше
                var stored = new StoredDocument
                {
                    FilePath = filePath,
                    Content = content,
                    Hash = hash,
                    LastOpened = DateTime.UtcNow
                };
                await _cacheService.AddOrUpdateAsync(stored);
            }

            return new DocumentModel
            {
                FilePath = filePath,
                Content = content
            };
        });
    }

    public async Task SaveDocumentAsync(string filePath, DocumentModel document)
    {
        // Сохраняем в файл
        using (var wordDoc = WordprocessingDocument.Open(filePath, true))
        {
            var body = wordDoc.MainDocumentPart?.Document?.Body;
            if (body != null)
            {
                body.RemoveAllChildren();
                var paragraphs = document.Content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                foreach (var paraText in paragraphs)
                {
                    var paragraph = new Paragraph();
                    var run = new Run();
                    run.AppendChild(new Text(paraText));
                    paragraph.AppendChild(run);
                    body.AppendChild(paragraph);
                }
            }
            wordDoc.MainDocumentPart?.Document?.Save();
        }

        // Обновляем кэш
        var newHash = LiteDbDocumentCache.ComputeHash(document.Content);
        var stored = new StoredDocument
        {
            FilePath = filePath,
            Content = document.Content,
            Hash = newHash,
            LastOpened = DateTime.UtcNow
        };
        await _cacheService.AddOrUpdateAsync(stored);
    }

    private string ComputeHash(string content)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}