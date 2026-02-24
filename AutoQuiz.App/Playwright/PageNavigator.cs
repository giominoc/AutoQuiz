using Microsoft.Playwright;
using Microsoft.Extensions.Logging;

namespace AutoQuiz.App.Playwright;

public class PageNavigator
{
    private readonly ILogger<PageNavigator> _logger;
    private readonly HashSet<string> _visitedMenuItems = new HashSet<string>();

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
                // English
                "button:has-text('Next')",
                "button:has-text('Continue')",
                "a:has-text('Next')",
                "a:has-text('Continue')",
                
                // Italian
                "button:has-text('Avanti')",
                "a:has-text('Avanti')",
                "button:has-text('Continua')",
                "a:has-text('Continua')",
                "button:has-text('Successivo')",
                "a:has-text('Successivo')",
                "button:has-text('Prosegui')",
                "a:has-text('Prosegui')",
                
                // SCORM navigation
                "[class*='next']",
                "[id*='next']",
                "[class*='forward']",
                "[id*='forward']",
                "button[title*='Next']",
                "button[title*='Avanti']",
                "[aria-label*='Next']",
                "[aria-label*='Avanti']",
                
                // Generic
                "[data-purpose='next-button']",
                ".next-button",
                ".btn-next"
            };

            foreach (var button in nextButtons)
            {
                var nextButton = await page.QuerySelectorAsync(button);
                if (nextButton != null && await nextButton.IsVisibleAsync())
                {
                    await nextButton.ClickAsync();
                    _logger.LogInformation("Clicked next button: {Button}", button);
                    await Task.Delay(1000);
                    try
                    {
                        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 5000 });
                    }
                    catch (TimeoutException)
                    {
                        // Some SCORM pages don't trigger network events, continue anyway
                        _logger.LogDebug("NetworkIdle timeout after clicking next, continuing...");
                    }
                    return true;
                }
            }

            // Fallback: Try SCORM menu navigation
            var menuNavSuccess = await TryScormMenuNavigationAsync(page);
            if (menuNavSuccess)
            {
                return true;
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

    private async Task<bool> TryScormMenuNavigationAsync(IPage page)
    {
        _logger.LogInformation("Attempting SCORM menu navigation...");

        try
        {
            // Look for SCORM menu items
            var menuSelectors = new[]
            {
                "[class*='menu'] [class*='item']",
                "[class*='nav'] [class*='item']",
                "[role='menuitem']",
                "ul li a",
                ".course-menu li",
                ".lesson-list li"
            };

            foreach (var selector in menuSelectors)
            {
                var menuItems = await page.QuerySelectorAllAsync(selector);
                
                if (menuItems.Count > 0)
                {
                    _logger.LogInformation("Found {Count} menu items with selector: {Selector}", menuItems.Count, selector);

                    // First pass: Look for Italian language items
                    var italianItem = await FindPreferredLanguageItemAsync(menuItems, new[] { "ITALIANO", "ITALIAN", "ITA " });
                    if (italianItem != null)
                    {
                        var text = await italianItem.TextContentAsync() ?? "";
                        _logger.LogInformation("Found Italian module, clicking: {Text}", text.Trim());
                        await italianItem.ClickAsync();
                        await Task.Delay(2000);
                        return true;
                    }

                    // Second pass: Find the first unlocked/available item that's not completed or current
                    foreach (var item in menuItems)
                    {
                        var classes = await item.GetAttributeAsync("class") ?? "";
                        var text = await item.TextContentAsync() ?? "";
                        
                        // Skip if we've already visited this item (to prevent loops)
                        var itemKey = $"{text.Trim()}|{classes}";
                        if (_visitedMenuItems.Contains(itemKey))
                        {
                            _logger.LogDebug("Skipping already visited menu item: {Text}", text.Trim());
                            continue;
                        }
                        
                        // Check if item is available (not locked/disabled and not completed)
                        var isLocked = classes.Contains("locked", StringComparison.OrdinalIgnoreCase) ||
                                      classes.Contains("disabled", StringComparison.OrdinalIgnoreCase);
                        
                        var isCompleted = classes.Contains("completed", StringComparison.OrdinalIgnoreCase) ||
                                         classes.Contains("completato", StringComparison.OrdinalIgnoreCase) ||
                                         classes.Contains("completata", StringComparison.OrdinalIgnoreCase) ||
                                         text.Contains("✓") ||
                                         text.Contains("✔");

                        // Check if item is the current/active item
                        var isCurrent = classes.Contains("current", StringComparison.OrdinalIgnoreCase) ||
                                       classes.Contains("active", StringComparison.OrdinalIgnoreCase) ||
                                       classes.Contains("selected", StringComparison.OrdinalIgnoreCase) ||
                                       text.Contains("Unità corrente", StringComparison.OrdinalIgnoreCase) ||
                                       text.Contains("Current unit", StringComparison.OrdinalIgnoreCase);

                        if (!isLocked && !isCompleted && !isCurrent && await item.IsVisibleAsync())
                        {
                            // Mark as visited
                            _visitedMenuItems.Add(itemKey);
                            
                            _logger.LogInformation("Clicking next menu item: {Text}", text.Trim());
                            await item.ClickAsync();
                            await Task.Delay(2000);
                            return true;
                        }
                    }
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error in SCORM menu navigation");
            return false;
        }
    }

    private async Task<IElementHandle?> FindPreferredLanguageItemAsync(IReadOnlyList<IElementHandle> menuItems, string[] languageKeywords)
    {
        foreach (var item in menuItems)
        {
            var text = await item.TextContentAsync() ?? "";
            var classes = await item.GetAttributeAsync("class") ?? "";
            
            // Check if item contains any of the language keywords
            foreach (var keyword in languageKeywords)
            {
                if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    // Make sure it's not locked, completed, or current
                    var isLocked = classes.Contains("locked", StringComparison.OrdinalIgnoreCase) ||
                                  classes.Contains("disabled", StringComparison.OrdinalIgnoreCase);
                    
                    var isCompleted = classes.Contains("completed", StringComparison.OrdinalIgnoreCase) ||
                                     classes.Contains("completato", StringComparison.OrdinalIgnoreCase) ||
                                     classes.Contains("completata", StringComparison.OrdinalIgnoreCase) ||
                                     text.Contains("✓") ||
                                     text.Contains("✔");

                    var isCurrent = classes.Contains("current", StringComparison.OrdinalIgnoreCase) ||
                                   classes.Contains("active", StringComparison.OrdinalIgnoreCase) ||
                                   classes.Contains("selected", StringComparison.OrdinalIgnoreCase) ||
                                   text.Contains("Unità corrente", StringComparison.OrdinalIgnoreCase) ||
                                   text.Contains("Current unit", StringComparison.OrdinalIgnoreCase);

                    if (!isLocked && !isCompleted && !isCurrent && await item.IsVisibleAsync())
                    {
                        return item;
                    }
                }
            }
        }
        
        return null;
    }
}
