using System.Text.Json;
using DocMind.Infrastructure.Configuration.Models;
using Microsoft.Extensions.Logging;

namespace DocMind.Infrastructure.Configuration.Providers;

public class JsonFileConfigurationProvider : IConfigurationProvider
{
    private readonly string _configFilePath;
    private readonly ILogger<JsonFileConfigurationProvider> _logger;
    
    public JsonFileConfigurationProvider(
        ILogger<JsonFileConfigurationProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var configDir = Path.Combine(appData, "DocMind", "Config");
        Directory.CreateDirectory(configDir);
        _configFilePath = Path.Combine(configDir, "appsettings.json");
    }
    
    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_configFilePath))
            {
                _logger.LogInformation("Configuration file not found, creating default settings");
                return await GetDefaultSettingsAsync(cancellationToken);
            }
            
            var json = await File.ReadAllTextAsync(_configFilePath, cancellationToken);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            });
            
            if (settings == null)
            {
                _logger.LogWarning("Failed to deserialize configuration, using defaults");
                return await GetDefaultSettingsAsync(cancellationToken);
            }
            
            _logger.LogDebug("Configuration loaded successfully from {Path}", _configFilePath);
            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration from {Path}", _configFilePath);
            return await GetDefaultSettingsAsync(cancellationToken);
        }
    }
    
    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            await File.WriteAllTextAsync(_configFilePath, json, cancellationToken);
            _logger.LogDebug("Configuration saved successfully to {Path}", _configFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration to {Path}", _configFilePath);
            throw;
        }
    }
    
    public Task<bool> ValidateAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        // Basic validation - we'll implement more comprehensive validation later
        if (settings == null)
            return Task.FromResult(false);
            
        if (string.IsNullOrEmpty(settings.Database.ConnectionString))
            return Task.FromResult(false);
            
        if (settings.Ai.ContextSize < 512 || settings.Ai.ContextSize > 8192)
            return Task.FromResult(false);
            
        return Task.FromResult(true);
    }
    
    public Task<AppSettings> GetDefaultSettingsAsync(CancellationToken cancellationToken = default)
    {
        var defaultSettings = new AppSettings
        {
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            Version = "1.0.0",
            Database = new DatabaseSettings
            {
                ConnectionString = "Data Source={AppData}\\DocMind\\documents.db",
                CacheRetentionDays = 30,
                MaxConnections = 10,
                EnableCompression = true,
                CompressionLevel = 6
            },
            Ai = new AiSettings
            {
                DefaultProvider = AiSettings.AiProvider.Local,
                LocalModelPath = "{AppDir}Models\\Phi-3-mini-4k-instruct-q4.gguf",
                ContextSize = 2048,
                UseGpu = false,
                Temperature = 0.2,
                MaxTokens = 512,
                TopP = 0.85,
                TopK = 30
            },
            Ui = new UiSettings
            {
                CurrentTheme = UiSettings.Theme.System,
                Language = "ru-RU",
                AutoSaveInterval = 300,
                RecentDocumentsLimit = 10,
                ShowLineNumbers = true,
                WordWrap = true,
                FontSize = 14,
                FontFamily = "Segoe UI"
            },
            Logging = new LoggingSettings
            {
                MinimumLevel = LoggingSettings.LogLevel.Information,
                LogFilePath = "{AppData}\\DocMind\\Logs\\docmind-.log",
                RollingInterval = LoggingSettings.RollingIntervalType.Day,
                RetainedFileCountLimit = 7,
                EnableConsoleLogging = true,
                EnableFileLogging = true
            },
            Features = new FeatureSettings
            {
                EnableAutoSave = true,
                EnableSpellCheck = true,
                EnableGrammarCheck = false,
                EnableTranslation = true,
                EnableSummarization = true,
                EnableParaphrasing = true
            }
        };
        
        return Task.FromResult(defaultSettings);
    }
}