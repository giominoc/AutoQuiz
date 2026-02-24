using Microsoft.Extensions.Logging;

namespace AutoQuiz.App.Services;

public class LoggerService
{
    private readonly ILogger<LoggerService> _logger;
    private readonly string _logFilePath;
    private readonly List<string> _logs = new();

    public LoggerService(ILogger<LoggerService> logger)
    {
        _logger = logger;
        var logDir = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
        Directory.CreateDirectory(logDir);
        
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        _logFilePath = Path.Combine(logDir, $"quiz_automation_{timestamp}.log");
    }

    public void Log(string message)
    {
        var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
        _logs.Add(logEntry);
        _logger.LogInformation(message);
        
        // Write to file
        File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
        
        // Also write to console for real-time feedback
        Console.WriteLine(logEntry);
    }

    public List<string> GetLogs() => new List<string>(_logs);

    public string GetLogFilePath() => _logFilePath;
}
