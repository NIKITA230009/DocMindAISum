using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DocMind.Core.Interfaces;
using DocMind.Desktop.ViewModels;
using DocMind.Desktop.Views;
using DocMind.Services.History;
using DocMind.Services.Services;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using System;
using System.Threading.Tasks;

namespace DocMind.Desktop
{
    public class App : Application
    {
        private IServiceProvider? _services;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // Настройка DI
            var services = new ServiceCollection();
            ConfigureServices(services);
            _services = services.BuildServiceProvider();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {

                 var cacheService = _services.GetRequiredService<IDocumentCacheService>();


                // Запускаем очистку на фоне
                _ = Task.Run(async () => await cacheService.CleanupOldEntriesAsync(TimeSpan.FromDays(30)));


                desktop.MainWindow = new MainWindow
                {
                    DataContext = _services.GetRequiredService<MainWindowViewModel>()
                };
            }
            base.OnFrameworkInitializationCompleted();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<IDocumentService, WordDocumentService>();
            services.AddSingleton<IDocumentCacheService, LiteDbDocumentCache>();
            // Регистрация Refit-клиента
            services.AddRefitClient<IAiApi>()
                .ConfigureHttpClient(c =>
                {
                    c.BaseAddress = new Uri("http://localhost:5000"); // замените на реальный адрес сервера
                    c.Timeout = TimeSpan.FromSeconds(30);
                });
            // Регистрация AI-сервиса
            services.AddSingleton<IAiService, AiService>();
            services.AddSingleton<IQueryHistoryService, LiteDbQueryHistoryService>();
            services.AddSingleton<ILocalAiService, LLamaSharpAiService>();
        }
    }
}