using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocMind.Core.Interfaces;
using DocMind.Core.Models;
using LiteDB;

namespace DocMind.Services.History;

public class LiteDbQueryHistoryService : IQueryHistoryService
{
    private readonly string _databasePath;
    private const string CollectionName = "history";

    public LiteDbQueryHistoryService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dbPath = System.IO.Path.Combine(appData, "DocMind", "history.db");
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dbPath)!);
        _databasePath = dbPath;
    }

    public Task AddAsync(QueryHistory entry)
    {
        using var db = new LiteDatabase(_databasePath);
        var col = db.GetCollection<QueryHistory>(CollectionName);
        entry.Timestamp = DateTime.UtcNow;
        col.Insert(entry);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<QueryHistory>> GetRecentAsync(int limit = 20)
    {
        using var db = new LiteDatabase(_databasePath);
        var col = db.GetCollection<QueryHistory>(CollectionName);
        var items = col.Query()
                       .OrderByDescending(x => x.Timestamp)
                       .Limit(limit)
                       .ToList();
        return Task.FromResult(items.AsEnumerable());
    }

    public Task ClearAsync()
    {
        using var db = new LiteDatabase(_databasePath);
        db.DropCollection(CollectionName);
        return Task.CompletedTask;
    }
}