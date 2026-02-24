using Microsoft.Playwright;
using Microsoft.Extensions.Logging;

namespace AutoQuiz.App.Services;

public class CourseLauncher
{
    private readonly ILogger<CourseLauncher> _logger;

    public CourseLauncher(ILogger<CourseLauncher> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Attempts to launch a SCORM/NOVOLEARN course by detecting and clicking the launch button,
    /// then handling popup/iframe navigation and clicking the inner start button.
    /// </summary>
    /// <param name="page">The current page</param>
    /// <returns>The page/frame context where the course content is loaded, or null if launch failed</returns>
    public async Task<IPage?> TryLaunchCourseAsync(IPage page)
    {
        _logger.LogInformation("Attempting to launch course...");

        try
        {
            // Step 1: Look for the launch button on the course page
            var launchButton = await DetectLaunchButtonAsync(page);
            
            if (launchButton == null)
            {
                _logger.LogInformation("No launch button detected, course may already be started or use different format");
                return page; // Return original page if no launch needed
            }

            _logger.LogInformation("Launch button found, clicking...");

            // Step 2: Handle popup if one opens
            var popupTask = page.Context.WaitForPageAsync(new()
            {
                Timeout = 5000 // Wait up to 5 seconds for popup
            });

            // Click the launch button
            await launchButton.ClickAsync();
            _logger.LogInformation("Launch button clicked");

            IPage? targetPage = null;
            
            try
            {
                // Wait for popup to open
                targetPage = await popupTask;
                _logger.LogInformation("Popup opened with URL: {Url}", targetPage.Url);
                
                // Wait for popup to load
                await targetPage.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 10000 });
            }
            catch (TimeoutException)
            {
                _logger.LogInformation("No popup detected, checking for iframe...");
                
                // If no popup, check for iframe
                targetPage = await DetectIframeContextAsync(page);
                
                if (targetPage == null)
                {
                    _logger.LogInformation("No popup or iframe detected, using original page");
                    targetPage = page;
                }
            }

            // Step 3: Click the inner "INIZIO" or start button
            var startSuccess = await TryClickStartButtonAsync(targetPage);
            
            if (startSuccess)
            {
                _logger.LogInformation("Successfully launched course, now in lesson context");
                return targetPage;
            }
            else
            {
                _logger.LogWarning("Could not find inner start button, but course may have auto-started");
                return targetPage;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during course launch");
            return page; // Return original page on error
        }
    }

    private async Task<IElementHandle?> DetectLaunchButtonAsync(IPage page)
    {
        // Common launch button selectors for NOVOLEARN and SCORM courses
        var launchSelectors = new[]
        {
            // Italian variants
            "button:has-text('Clicca per iniziare')",
            "a:has-text('Clicca per iniziare')",
            "button:has-text('Inizia')",
            "a:has-text('Inizia')",
            "button:has-text('Avvia')",
            "a:has-text('Avvia')",
            "button:has-text('Comincia')",
            "a:has-text('Comincia')",
            
            // English variants
            "button:has-text('Click to start')",
            "a:has-text('Click to start')",
            "button:has-text('Start')",
            "a:has-text('Start')",
            "button:has-text('Launch')",
            "a:has-text('Launch')",
            "button:has-text('Begin')",
            "a:has-text('Begin')",
            
            // SCORM specific
            "[class*='scorm-launch']",
            "[id*='scorm-launch']",
            "[class*='launch-btn']",
            "[id*='launch-btn']"
        };

        foreach (var selector in launchSelectors)
        {
            var element = await page.QuerySelectorAsync(selector);
            if (element != null)
            {
                var text = await element.TextContentAsync();
                _logger.LogInformation("Found launch button with selector '{Selector}': {Text}", selector, text?.Trim());
                return element;
            }
        }

        return null;
    }

    private async Task<IPage?> DetectIframeContextAsync(IPage page)
    {
        _logger.LogInformation("Checking for SCORM iframe...");

        try
        {
            // Wait a bit for iframe to load
            await Task.Delay(2000);

            // Look for frames containing SCORM content
            var frames = page.Frames;
            
            foreach (var frame in frames)
            {
                var frameUrl = frame.Url;
                
                // Check if frame URL contains SCORM indicators
                if (frameUrl.Contains("index_lms.html", StringComparison.OrdinalIgnoreCase) ||
                    frameUrl.Contains("scorm", StringComparison.OrdinalIgnoreCase) ||
                    frameUrl.Contains("content", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Found SCORM iframe with URL: {Url}", frameUrl);
                    
                    // For iframes, we need to work with frames directly, not IPage
                    // Return the main page and we'll handle frame operations in the calling code
                    return page;
                }
            }

            _logger.LogInformation("No SCORM iframe detected");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting iframe context");
            return null;
        }
    }

    private async Task<bool> TryClickStartButtonAsync(IPage page)
    {
        _logger.LogInformation("Looking for inner start button (e.g., INIZIO)...");

        var startSelectors = new[]
        {
            // Italian
            "button:has-text('INIZIO')",
            "a:has-text('INIZIO')",
            "button:has-text('Inizia')",
            "a:has-text('Inizia')",
            "button:has-text('Avvia')",
            "a:has-text('Avvia')",
            "[value='INIZIO']",
            
            // English
            "button:has-text('START')",
            "a:has-text('START')",
            "button:has-text('Begin')",
            "a:has-text('Begin')",
            "[value='START']",
            
            // Generic SCORM start
            "[class*='start-btn']",
            "[id*='start-btn']",
            "[class*='begin']",
            "[id*='begin']"
        };

        // First try on the popup page directly
        foreach (var selector in startSelectors)
        {
            try
            {
                var element = await page.QuerySelectorAsync(selector);
                if (element != null)
                {
                    var text = await element.TextContentAsync();
                    _logger.LogInformation("Found start button: {Text}", text?.Trim());
                    
                    await element.ClickAsync();
                    _logger.LogInformation("Start button clicked");
                    
                    // Wait for navigation/content to load
                    await Task.Delay(2000);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error clicking start button with selector: {Selector}", selector);
            }
        }

        // If not found on page, try in iframes
        var frames = page.Frames;
        foreach (var frame in frames)
        {
            foreach (var selector in startSelectors)
            {
                try
                {
                    var element = await frame.QuerySelectorAsync(selector);
                    if (element != null)
                    {
                        var text = await element.TextContentAsync();
                        _logger.LogInformation("Found start button in frame: {Text}", text?.Trim());
                        
                        await element.ClickAsync();
                        _logger.LogInformation("Start button clicked in frame");
                        
                        await Task.Delay(2000);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error clicking start button in frame with selector: {Selector}", selector);
                }
            }
        }

        _logger.LogInformation("No start button found");
        return false;
    }

    /// <summary>
    /// Gets the appropriate frame for SCORM content, or returns the page itself if no frame is found
    /// </summary>
    public IFrameLocator? GetScormFrame(IPage page)
    {
        try
        {
            var frames = page.Frames;
            
            foreach (var frame in frames)
            {
                var frameUrl = frame.Url;
                
                if (frameUrl.Contains("index_lms.html", StringComparison.OrdinalIgnoreCase) ||
                    frameUrl.Contains("scorm", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Using SCORM frame with URL: {Url}", frameUrl);
                    return page.FrameLocator($"[src*='{new Uri(frameUrl).PathAndQuery}']");
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SCORM frame");
            return null;
        }
    }
}
