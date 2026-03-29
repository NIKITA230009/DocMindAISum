using DocMind.Core.Interfaces;
using DocMind.Core.Models;
using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DocMind.Services.Services
{
    public class LiteDbDocumentCache : IDocumentCacheService
    {
        private readonly string _databasePath;
        private readonly object _lock = new object();

        public LiteDbDocumentCache()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dbFolder = Path.Combine(appData, "DocMind");
            Directory.CreateDirectory(dbFolder);
            _databasePath = Path.Combine(dbFolder, "documents.db");
        }

        public Task AddOrUpdateAsync(StoredDocument document)
        {
            return Task.Run(() =>
            {
                using var db = new LiteDatabase(_databasePath);
                var col = db.GetCollection<StoredDocument>("documents");
                col.Upsert(document); // если Id совпадает, обновит, иначе вставит
            });
        }

        public Task<StoredDocument?> GetByPathAsync(string filePath)
        {
            return Task.Run(() =>
            {
                using var db = new LiteDatabase(_databasePath);
                var col = db.GetCollection<StoredDocument>("documents");
                return col?.FindOne(x => x.FilePath == filePath);
            });
        }

        public Task<List<StoredDocument>> GetRecentAsync(int count = 10)
        {
            return Task.Run(() =>
            {
                using var db = new LiteDatabase(_databasePath);
                var col = db.GetCollection<StoredDocument>("documents");
                return col.Query()
                    .OrderByDescending(x => x.LastOpened)
                    .Limit(count)
                    .ToList();
            });
        }

        public Task RemoveAsync(string filePath)
        {
            return Task.Run(() =>
            {
                using var db = new LiteDatabase(_databasePath);
                var col = db.GetCollection<StoredDocument>("documents");
                col.DeleteMany(x => x.FilePath == filePath);
            });
        }
        // Вспомогательный метод для вычисления хеша строки (можно вынести)
        public static string ComputeHash(string text)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(text);
            var hashBytes = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }

        public Task CleanupOldEntriesAsync(TimeSpan maxAge)
        {
            return Task.Run(() =>
            {
                using var db = new LiteDatabase(_databasePath);
                var col = db.GetCollection<StoredDocument>("documents");
                var cutoff = DateTime.UtcNow - maxAge;
                col.DeleteMany(x => x.LastOpened < cutoff);
            });
        }
    }
}