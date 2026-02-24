using Microsoft.Playwright;
using Microsoft.Extensions.Logging;

namespace AutoQuiz.App.Playwright;

public class PageNavigator
{
    private readonly ILogger<PageNavigator> _logger;

    public PageNavigator(ILogger<PageNavigator> logger)
    {
        _logger = logger;
    }

    public async Task NavigateToAsync(IPage page, string url)
    {
        _logger.LogInformation("Navigating to: {Url}", url);
        
        try
        {
            await page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 60000
            });

            _logger.LogInformation("Navigation completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Navigation failed");
            throw;
        }
    }

    public async Task<bool> TryClickNextAsync(IPage page)
    {
        try
        {
            var nextButtons = new[]
            {
                "button:has-text('Next')",
                "[data-purpose='next-button']",
                ".next-button",
                "button:has-text('Continue')",
                "a:has-text('Next')"
            };

            foreach (var button in nextButtons)
            {
                var nextButton = await page.QuerySelectorAsync(button);
                if (nextButton != null)
                {
                    await nextButton.ClickAsync();
                    _logger.LogInformation("Clicked next button: {Button}", button);
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                    return true;
                }
            }

            _logger.LogInformation("No next button found");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error clicking next button");
            return false;
        }
    }
}
