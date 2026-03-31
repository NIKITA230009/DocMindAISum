using System.Reactive.Subjects;
using DocMind.Infrastructure.Configuration.Models;
using DocMind.Infrastructure.Configuration.Providers;
using Microsoft.Extensions.Logging;

namespace DocMind.Infrastructure.Configuration.Services;

public class ConfigurationService : IConfigurationService, IDisposable
{
    private readonly IConfigurationProvider _provider;
    private readonly ILogger<ConfigurationService> _logger;
    private readonly BehaviorSubject<AppSettings> _settingsSubject;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _disposed;
    
    public ConfigurationService(
        IConfigurationProvider provider,
        ILogger<ConfigurationService> logger)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Initialize with default settings
        var defaultSettings = _provider.GetDefaultSettingsAsync().GetAwaiter().GetResult();
        _settingsSubject = new BehaviorSubject<AppSettings>(defaultSettings);
    }
    
    public AppSettings CurrentSettings => _settingsSubject.Value;
    public IObservable<AppSettings> SettingsChanged => _settingsSubject;
    
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var settings = await _provider.LoadAsync(cancellationToken);
            _settingsSubject.OnNext(settings);
            _logger.LogInformation("Configuration loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration");
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }
    
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await _provider.SaveAsync(CurrentSettings, cancellationToken);
            _logger.LogDebug("Configuration saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration");
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }
    
    public async Task ResetToDefaultsAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var defaultSettings = await _provider.GetDefaultSettingsAsync(cancellationToken);
            _settingsSubject.OnNext(defaultSettings);
            await SaveAsync(cancellationToken);
            _logger.LogInformation("Configuration reset to defaults");
        }
        finally
        {
            _lock.Release();
        }
    }
    
    public async Task UpdateAsync(Action<AppSettings> updateAction, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var settings = CurrentSettings;
            updateAction(settings);
            _settingsSubject.OnNext(settings);
            await SaveAsync(cancellationToken);
            _logger.LogDebug("Configuration updated");
        }
        finally
        {
            _lock.Release();
        }
    }
    
    public Task<T> GetSectionAsync<T>(Func<AppSettings, T> selector, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(selector(CurrentSettings));
    }
    
    public async Task ValidateAsync(CancellationToken cancellationToken = default)
    {
        var isValid = await _provider.ValidateAsync(CurrentSettings, cancellationToken);
        if (!isValid)
        {
            _logger.LogWarning("Configuration validation failed");
            throw new InvalidOperationException("Configuration validation failed");
        }
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _settingsSubject?.Dispose();
            _lock?.Dispose();
            _disposed = true;
        }
    }
}