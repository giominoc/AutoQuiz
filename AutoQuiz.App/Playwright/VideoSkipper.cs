using Microsoft.Playwright;
using Microsoft.Extensions.Logging;

namespace AutoQuiz.App.Playwright;

public class VideoSkipper
{
    private readonly ILogger<VideoSkipper> _logger;

    public VideoSkipper(ILogger<VideoSkipper> logger)
    {
        _logger = logger;
    }

    public async Task SkipVideoIfPresentAsync(IPage page, int delayMs = 2000)
    {
        try
        {
            _logger.LogInformation("Checking for video to skip...");

            // Common video player selectors
            var videoSelectors = new[]
            {
                "video",
                ".video-player",
                "[data-purpose='video-player']",
                "#player",
                ".vjs-tech"
            };

            foreach (var selector in videoSelectors)
            {
                var videoElement = await page.QuerySelectorAsync(selector);
                if (videoElement != null)
                {
                    _logger.LogInformation("Video found with selector: {Selector}", selector);

                    // Try to skip or fast-forward
                    await TrySkipVideoAsync(page, videoElement);
                    await Task.Delay(delayMs);
                    return;
                }
            }

            _logger.LogInformation("No video found to skip");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while trying to skip video");
        }
    }

    private async Task TrySkipVideoAsync(IPage page, IElementHandle videoElement)
    {
        try
        {
            // Try to find and click skip button
            var skipButtons = new[]
            {
                "button:has-text('Skip')",
                "[data-purpose='skip-button']",
                ".skip-button",
                "button:has-text('Next')"
            };

            foreach (var button in skipButtons)
            {
                var skipButton = await page.QuerySelectorAsync(button);
                if (skipButton != null)
                {
                    await skipButton.ClickAsync();
                    _logger.LogInformation("Clicked skip button: {Button}", button);
                    return;
                }
            }

            // If no skip button, try to seek to end of video
            await page.EvaluateAsync(@"
                const video = document.querySelector('video');
                if (video) {
                    video.currentTime = video.duration - 1;
                }
            ");
            _logger.LogInformation("Fast-forwarded video to end");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not skip video");
        }
    }
}
