using DocMind.Infrastructure.Configuration.Models;

namespace DocMind.Infrastructure.Configuration.Providers;

public interface IConfigurationProvider
{
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
    Task<bool> ValidateAsync(AppSettings settings, CancellationToken cancellationToken = default);
    Task<AppSettings> GetDefaultSettingsAsync(CancellationToken cancellationToken = default);
}