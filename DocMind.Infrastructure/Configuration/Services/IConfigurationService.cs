using DocMind.Infrastructure.Configuration.Models;

namespace DocMind.Infrastructure.Configuration.Services;

public interface IConfigurationService
{
    AppSettings CurrentSettings { get; }
    IObservable<AppSettings> SettingsChanged { get; }
    
    Task LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(CancellationToken cancellationToken = default);
    Task ResetToDefaultsAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(Action<AppSettings> updateAction, CancellationToken cancellationToken = default);
    Task<T> GetSectionAsync<T>(Func<AppSettings, T> selector, CancellationToken cancellationToken = default);
    Task ValidateAsync(CancellationToken cancellationToken = default);
}