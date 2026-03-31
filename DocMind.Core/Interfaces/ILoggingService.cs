namespace DocMind.Core.Interfaces;

public interface ILoggingService
{
    void LogVerbose(string message, params object[] args);
    void LogDebug(string message, params object[] args);
    void LogInformation(string message, params object[] args);
    void LogWarning(string message, params object[] args);
    void LogError(string message, params object[] args);
    void LogError(Exception exception, string message, params object[] args);
    void LogFatal(string message, params object[] args);
    void LogFatal(Exception exception, string message, params object[] args);
    
    IDisposable BeginScope<TState>(TState state);
}