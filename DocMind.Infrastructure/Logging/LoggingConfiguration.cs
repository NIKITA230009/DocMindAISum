using DocMind.Infrastructure.Configuration.Models;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace DocMind.Infrastructure.Logging;

public static class LoggingConfiguration
{
    public static LoggerConfiguration CreateDefaultConfiguration()
    {
        return new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                theme: AnsiConsoleTheme.Code);
    }
    
    public static LoggerConfiguration ConfigureFromSettings(
        this LoggerConfiguration loggerConfiguration,
        LoggingSettings settings)
    {
        if (settings == null)
            return loggerConfiguration;
        
        // Set minimum level
        loggerConfiguration.MinimumLevel.Is(settings.SerilogLevel);
        
        // Configure console logging
        if (settings.EnableConsoleLogging)
        {
            loggerConfiguration.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                theme: AnsiConsoleTheme.Code,
                restrictedToMinimumLevel: settings.SerilogLevel);
        }
        
        // Configure file logging
        if (settings.EnableFileLogging && !string.IsNullOrEmpty(settings.ResolvedLogFilePath))
        {
            loggerConfiguration.WriteTo.File(
                path: settings.ResolvedLogFilePath,
                rollingInterval: ConvertRollingInterval(settings.RollingInterval),
                retainedFileCountLimit: settings.RetainedFileCountLimit,
                fileSizeLimitBytes: settings.FileSizeLimitBytes,
                buffered: settings.Buffered,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                restrictedToMinimumLevel: settings.SerilogLevel);
        }
        
        return loggerConfiguration;
    }
    
    private static Serilog.RollingInterval ConvertRollingInterval(LoggingSettings.RollingIntervalType interval)
    {
        return interval switch
        {
            LoggingSettings.RollingIntervalType.Infinite => Serilog.RollingInterval.Infinite,
            LoggingSettings.RollingIntervalType.Year => Serilog.RollingInterval.Year,
            LoggingSettings.RollingIntervalType.Month => Serilog.RollingInterval.Month,
            LoggingSettings.RollingIntervalType.Day => Serilog.RollingInterval.Day,
            LoggingSettings.RollingIntervalType.Hour => Serilog.RollingInterval.Hour,
            LoggingSettings.RollingIntervalType.Minute => Serilog.RollingInterval.Minute,
            _ => Serilog.RollingInterval.Day
        };
    }
    
    public static ILogger CreateLogger(LoggingSettings settings)
    {
        var configuration = CreateDefaultConfiguration();
        configuration.ConfigureFromSettings(settings);
        return configuration.CreateLogger();
    }
}