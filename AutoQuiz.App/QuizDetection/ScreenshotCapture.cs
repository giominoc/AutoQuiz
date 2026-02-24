using Microsoft.Playwright;
using Microsoft.Extensions.Logging;

namespace AutoQuiz.App.QuizDetection;

public class ScreenshotCapture
{
    private readonly ILogger<ScreenshotCapture> _logger;
    private readonly string _screenshotDir;

    public ScreenshotCapture(ILogger<ScreenshotCapture> logger)
    {
        _logger = logger;
        _screenshotDir = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "Screenshots");
        Directory.CreateDirectory(_screenshotDir);
    }

    public async Task<string> CaptureAsync(IPage page, int questionNumber)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var filename = $"question_{questionNumber}_{timestamp}.png";
            var filepath = Path.Combine(_screenshotDir, filename);

            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = filepath,
                FullPage = false
            });

            _logger.LogInformation("Screenshot captured: {Path}", filepath);
            return filepath;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to capture screenshot");
            return string.Empty;
        }
    }
}
