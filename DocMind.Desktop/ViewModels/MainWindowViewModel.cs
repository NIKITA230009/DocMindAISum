using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocMind.Core.Dto;
using DocMind.Core.Interfaces;
using DocMind.Core.Models;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
namespace DocMind.Desktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IDocumentService _documentService;
    private readonly IDocumentCacheService _cacheService;
    private StoredDocument? _selectedDocument;
    private readonly IAiService _aiService;
    private readonly IQueryHistoryService _historyService;
    private readonly ILocalAiService _localAiService;


    [ObservableProperty]
    private ObservableCollection<StoredDocument> _recentDocuments = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    private string _documentContent = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _aiInputText = string.Empty;

    [ObservableProperty]
    private string _aiOutputText = string.Empty;

    [ObservableProperty]
    private bool _isAiBusy;

    [ObservableProperty]
    private string _statusText = "Готов";

    [ObservableProperty]
    private string _aiCommand = string.Empty; // поле для ввода команды

    [ObservableProperty]
    private ObservableCollection<QueryHistory> _queryHistory = new();

    private DocumentModel? _currentDocument;

    public StoredDocument? SelectedDocument
    {
        get => _selectedDocument;
        set => SetProperty(ref _selectedDocument, value); 
    }

    public MainWindowViewModel(IDocumentService documentService, ILocalAiService localAiService, IQueryHistoryService historyService, IAiService aiService, IDocumentCacheService cacheService)
    {
        _documentService = documentService;
        Console.WriteLine("[VM] Конструктор вызван");
        _cacheService = cacheService;
        LoadRecentDocumentsAsync().ConfigureAwait(false);
        _aiService = aiService;
        _historyService = historyService;
        _localAiService = localAiService;

    }

    // Метод загрузки истории
    private async Task LoadRecentDocumentsAsync()
    {
        var docs = await _cacheService.GetRecentAsync();
        RecentDocuments = new ObservableCollection<StoredDocument>(docs);
    }

    // Команда для открытия из истории (привязать к двойному клику или кнопке)
    [RelayCommand]
    private async Task OpenFromHistory()
    {
        if (SelectedDocument != null)
        {
            await LoadDocumentAsync(SelectedDocument.FilePath);
        }
    }

    [RelayCommand]
    private async Task OpenDocument()
    {
        var topLevel = GetTopLevel();
        if (topLevel == null)
        {
            StatusText = "Не удалось получить доступ к окну";
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Открыть документ Word",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Документы Word")
                {
                    Patterns = new[] { "*.docx", "*.doc" }
                }
            }
        });

        if (files.Count >= 1)
        {
            var file = files[0];
            var path = file.Path.LocalPath;
            await LoadDocumentAsync(path);
        }
    }

    [RelayCommand]
    private async Task SaveDocument()
    {
        if (_currentDocument == null)
        {
            StatusText = "Нет открытого документа";
            return;
        }

        IsBusy = true;
        StatusText = "Сохранение...";

        try
        {
            _currentDocument.Content = DocumentContent;
            await _documentService.SaveDocumentAsync(_currentDocument.FilePath, _currentDocument);
            StatusText = $"Сохранено: {_currentDocument.FilePath}";
        }
        catch (Exception ex)
        {
            StatusText = $"Ошибка сохранения: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    [RelayCommand]
    private async Task AiSummarize()
    {
        if (string.IsNullOrWhiteSpace(AiInputText))
            return;

        IsAiBusy = true;
        AiOutputText = "Ожидание ответа...";
        try
        {
            var result = await _aiService.SummarizeAsync(AiInputText);
            AiOutputText = result;
        }
        catch (Exception ex)
        {
            AiOutputText = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsAiBusy = false;
        }
    }

    private async Task LoadDocumentAsync(string path)
    {
        IsBusy = true;
        StatusText = $"Загрузка {path}...";
        Debug.WriteLine($"[VM] LoadDocumentAsync начат, путь: {path}");

        try
        {
            var document = await _documentService.LoadDocumentAsync(path);
            _currentDocument = document;
            DocumentContent = document.Content;
            //OnPropertyChanged(nameof(DocumentContent));
            Debug.WriteLine($"[VM] DocumentContent установлен, длина = {DocumentContent.Length}");
            StatusText = $"Загружен: {document.FilePath}";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VM] Ошибка загрузки: {ex}");
            StatusText = $"Ошибка загрузки: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    private TopLevel? GetTopLevel()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        return null;
    }

    private async Task LoadQueryHistoryAsync()
    {
        var history = await _historyService.GetRecentAsync();
        QueryHistory = new ObservableCollection<QueryHistory>(history);
    }

    [RelayCommand]
    private async Task ExecuteAiCommand()
    {
        if (string.IsNullOrWhiteSpace(AiCommand) || string.IsNullOrWhiteSpace(DocumentContent))
            return;

        string textToSend = DocumentContent; // позже можно будет отправлять выделенный текст

        IsAiBusy = true;
        AiOutputText = "Обработка...";

        try
        {
            // Используем локальный сервис
            var result = await _localAiService.ExecuteCommandAsync(textToSend, AiCommand);
            AiOutputText = result;

            // Сохраняем в историю
            var historyEntry = new QueryHistory
            {
                DocumentPath = _currentDocument?.FilePath ?? "",
                InputText = textToSend.Length > 100 ? textToSend[..100] + "..." : textToSend,
                Command = AiCommand,
                Response = result.Length > 100 ? result[..100] + "..." : result
            };
            await _historyService.AddAsync(historyEntry);
            await LoadQueryHistoryAsync();
        }
        catch (Exception ex)
        {
            AiOutputText = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsAiBusy = false;
        }
    }

    //[RelayCommand]
    //private async Task ExecuteAiCommand()
    //{
    //    if (string.IsNullOrWhiteSpace(AiCommand) || string.IsNullOrWhiteSpace(DocumentContent))
    //        return;

    //    // Определим, какой текст отправлять: выделенный или весь
    //    string textToSend = DocumentContent; // пока весь, позже можно улучшить

    //    IsAiBusy = true;
    //    AiOutputText = "Обработка...";

    //    try
    //    {
    //        var result = await _aiService.ExecuteCommandAsync(textToSend, AiCommand, CancellationToken.None);
    //        AiOutputText = result;

    //        // Сохраняем в историю
    //        var historyEntry = new QueryHistory
    //        {
    //            DocumentPath = _currentDocument?.FilePath ?? "",
    //            InputText = textToSend.Length > 100 ? textToSend[..100] + "..." : textToSend,
    //            Command = AiCommand,
    //            Response = result.Length > 100 ? result[..100] + "..." : result
    //        };
    //        await _historyService.AddAsync(historyEntry);
    //        await LoadQueryHistoryAsync(); // обновляем список
    //    }
    //    catch (Exception ex)
    //    {
    //        AiOutputText = $"Ошибка: {ex.Message}";
    //    }
    //    finally
    //    {
    //        IsAiBusy = false;
    //    }
    //}

    [RelayCommand]
    private void InsertAiResult()
    {
        // Заменяем выделенный текст (или весь) на результат
        DocumentContent = AiOutputText;
    }

    [RelayCommand]
    private void SetSummarize()
    {
        AiCommand = "суммаризируй";
        ExecuteAiCommandCommand.Execute(null); // закомментируйте
    }

    [RelayCommand]
    private void SetRephrase()
    {
        AiCommand = "перефразируй";
        ExecuteAiCommandCommand.Execute(null);
    }
}