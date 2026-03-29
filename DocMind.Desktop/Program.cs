using Avalonia;
using Serilog;
using System;

namespace DocMind.Desktop;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Настройка Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs/docmind-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            Log.Information("Запуск приложения");
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Приложение завершилось с критической ошибкой");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
             .WithInterFont() 
            .LogToTrace();
}