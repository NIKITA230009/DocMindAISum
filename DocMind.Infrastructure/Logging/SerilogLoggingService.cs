using DocMind.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace DocMind.Infrastructure.Logging;

public class SerilogLoggingService : ILoggingService
{
    private readonly ILogger<SerilogLoggingService> _logger;
    
    public SerilogLoggingService(ILogger<SerilogLoggingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public void LogVerbose(string message, params object[] args)
    {
        _logger.LogTrace(message, args);
    }
    
    public void LogDebug(string message, params object[] args)
    {
        _logger.LogDebug(message, args);
    }
    
    public void LogInformation(string message, params object[] args)
    {
        _logger.LogInformation(message, args);
    }
    
    public void LogWarning(string message, params object[] args)
    {
        _logger.LogWarning(message, args);
    }
    
    public void LogError(string message, params object[] args)
    {
        _logger.LogError(message, args);
    }
    
    public void LogError(Exception exception, string message, params object[] args)
    {
        _logger.LogError(exception, message, args);
    }
    
    public void LogFatal(string message, params object[] args)
    {
        _logger.LogCritical(message, args);
    }
    
    public void LogFatal(Exception exception, string message, params object[] args)
    {
        _logger.LogCritical(exception, message, args);
    }
    
    public IDisposable BeginScope<TState>(TState state)
    {
        return _logger.BeginScope(state);
    }
}