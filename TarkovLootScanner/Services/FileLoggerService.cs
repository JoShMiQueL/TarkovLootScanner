using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace TarkovLootScanner.Services;

/// <summary>
/// File-based logger implementation
/// </summary>
public class FileLoggerService : ILoggerService
{
    private readonly string _logFilePath;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public FileLoggerService()
    {
        var exePath = AppDomain.CurrentDomain.BaseDirectory;
        var logDir = Path.Combine(exePath, "logs");
        Directory.CreateDirectory(logDir);

        var latestLogPath = Path.Combine(logDir, "latest.txt");

        // Archive previous latest.txt if it exists
        if (File.Exists(latestLogPath))
        {
            try
            {
                // Create compressed archive of the previous log
                var archiveName = $"tarkov_log_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
                var archivePath = Path.Combine(logDir, archiveName);

                // Compress the log file using ZIP
                CompressLogFile(latestLogPath, archivePath);

                // Remove the original file after successful compression
                if (File.Exists(archivePath))
                {
                    File.Delete(latestLogPath);
                    Debug.WriteLine($"Compressed and archived previous log to: {archiveName}");
                }
                else
                {
                    // Fallback: just rename if compression fails
                    var fallbackName = $"tarkov_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                    var fallbackPath = Path.Combine(logDir, fallbackName);
                    File.Move(latestLogPath, fallbackPath);
                    Debug.WriteLine($"Fallback: moved previous log to: {fallbackName}");
                }
            }
            catch (Exception ex)
            {
                // If archiving fails, log the error but continue
                Debug.WriteLine($"Failed to archive previous log: {ex.Message}");
            }
        }

        _logFilePath = latestLogPath;
    }

    private void CompressLogFile(string logFilePath, string archivePath)
    {
        try
        {
            // Create a ZIP archive containing the log file
            using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
            {
                archive.CreateEntryFromFile(logFilePath, Path.GetFileName(logFilePath));
            }
        }
        catch (Exception ex)
        {
            // If compression fails, the caller will handle fallback
            Debug.WriteLine($"Compression failed: {ex.Message}");
            throw;
        }
    }

    private async Task WriteLogAsync(string level, string message)
    {
        var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
        
        Debug.WriteLine(logEntry);
        
        await _semaphore.WaitAsync();
        try
        {
            await File.AppendAllTextAsync(_logFilePath, logEntry + Environment.NewLine);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void LogInformation(string message)
    {
        _ = WriteLogAsync("INFO", message);
    }

    public void LogWarning(string message)
    {
        _ = WriteLogAsync("WARN", message);
    }

    public void LogError(string message)
    {
        _ = WriteLogAsync("ERROR", message);
    }

    public void LogError(string message, Exception ex)
    {
        var fullMessage = $"{message} | Exception: {ex.Message} | StackTrace: {ex.StackTrace}";
        _ = WriteLogAsync("ERROR", fullMessage);
    }

    public async Task LogPerformanceAsync(string operation, TimeSpan duration, string? additionalInfo = null)
    {
        var message = $"PERFORMANCE - Operation: {operation} | Duration: {duration.TotalMilliseconds}ms";
        if (!string.IsNullOrEmpty(additionalInfo))
        {
            message += $" | Info: {additionalInfo}";
        }
        await WriteLogAsync("PERF", message);
    }

    public async Task LogAPICallAsync(string endpoint, TimeSpan duration, bool success, string? errorMessage = null)
    {
        var status = success ? "SUCCESS" : "FAILED";
        var message = $"API - Endpoint: {endpoint} | Duration: {duration.TotalMilliseconds}ms | Status: {status}";
        if (!string.IsNullOrEmpty(errorMessage))
        {
            message += $" | Error: {errorMessage}";
        }
        await WriteLogAsync("API", message);
    }

    public async Task LogCacheOperationAsync(string operation, string key, bool success, TimeSpan? duration = null)
    {
        var status = success ? "SUCCESS" : "FAILED";
        var message = $"CACHE - Operation: {operation} | Key: {key} | Status: {status}";
        if (duration.HasValue)
        {
            message += $" | Duration: {duration.Value.TotalMilliseconds}ms";
        }
        await WriteLogAsync("CACHE", message);
    }
}
