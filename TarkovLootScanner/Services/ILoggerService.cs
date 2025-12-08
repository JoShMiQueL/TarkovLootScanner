namespace TarkovLootScanner.Services;

/// <summary>
/// Interface for logging service
/// </summary>
public interface ILoggerService
{
    void LogInformation(string message);
    void LogWarning(string message); 
    void LogError(string message);
    void LogError(string message, Exception ex);
    Task LogPerformanceAsync(string operation, TimeSpan duration, string? additionalInfo = null);
    Task LogAPICallAsync(string endpoint, TimeSpan duration, bool success, string? errorMessage = null);
    Task LogCacheOperationAsync(string operation, string key, bool success, TimeSpan? duration = null);
}
