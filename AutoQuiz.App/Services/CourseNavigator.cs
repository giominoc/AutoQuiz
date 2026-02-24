using Microsoft.Playwright;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace AutoQuiz.App.Services;

public class CourseNavigator
{
    private readonly ILogger<CourseNavigator> _logger;
    private static readonly Regex PercentageRegex = new Regex(@"(\d+(?:\.\d+)?)\s*%", RegexOptions.Compiled);

    public CourseNavigator(ILogger<CourseNavigator> logger)
    {
        _logger = logger;
    }

    public async Task<List<string>> GetIncompleteCoursesAsync(IPage page)
    {
        _logger.LogInformation("Looking for incomplete courses");

        try
        {
            var incompleteCourses = new List<string>();

            // Common selectors for course lists with incomplete status
            var courseSelectors = new[]
            {
                // Generic course links
                "a[href*='/course/']",
                "a[href*='/courses/']",
                ".course-card a",
                ".course-item a",
                "[data-purpose='course-card'] a",
                
                // Course containers
                ".course-card",
                ".course-item",
                "[data-purpose='course-card']"
            };

            // Incomplete course indicators
            var incompleteIndicators = new[]
            {
                ":not(:has-text('Completed'))",
                ":not(:has-text('100%'))",
                ":has-text('In Progress')",
                ":has-text('Continue')",
                ":has-text('Start')"
            };

            foreach (var selector in courseSelectors)
            {
                var elements = await page.QuerySelectorAllAsync(selector);
                
                if (elements.Count > 0)
                {
                    _logger.LogInformation("Found {Count} potential courses with selector: {Selector}", elements.Count, selector);

                    foreach (var element in elements)
                    {
                        // Check if course is incomplete by looking for progress indicators
                        var text = await element.TextContentAsync();
                        var href = await element.GetAttributeAsync("href");

                        if (!string.IsNullOrEmpty(href))
                        {
                            // Check if it's not completed
                            if (text != null && !IsComplete(text))
                            {
                                var fullUrl = href.StartsWith("http") ? href : new Uri(new Uri(page.Url), href).ToString();
                                
                                if (!incompleteCourses.Contains(fullUrl))
                                {
                                    incompleteCourses.Add(fullUrl);
                                    _logger.LogInformation("Found incomplete course: {Url} (Progress: {Text})", fullUrl, text.Trim());
                                }
                            }
                        }
                    }

                    if (incompleteCourses.Count > 0)
                    {
                        break;
                    }
                }
            }

            _logger.LogInformation("Total incomplete courses found: {Count}", incompleteCourses.Count);
            return incompleteCourses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting incomplete courses");
            return new List<string>();
        }
    }

    public async Task<bool> NavigateToCourseAsync(IPage page, string courseUrl)
    {
        _logger.LogInformation("Navigating to course: {Url}", courseUrl);

        try
        {
            await page.GotoAsync(courseUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 60000
            });

            _logger.LogInformation("Successfully navigated to course");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to course");
            return false;
        }
    }

    public async Task<bool> IsCourseCompleteAsync(IPage page)
    {
        try
        {
            // Get the page text content to check for completion
            var bodyText = await page.TextContentAsync("body");
            
            if (!string.IsNullOrEmpty(bodyText))
            {
                var isComplete = IsComplete(bodyText);
                if (isComplete)
                {
                    _logger.LogInformation("Course appears to be complete");
                    return true;
                }
            }

            // Also check for specific completion indicators in the UI
            var completionSelectors = new[]
            {
                ":has-text('Completed')",
                ":has-text('Completato')",      // Italian
                ":has-text('Completata')",      // Italian (feminine)
                ":has-text('100%')",
                ":has-text('Certificate')",
                ":has-text('Certificato')",     // Italian
                "[data-purpose='completion-indicator']"
            };

            foreach (var selector in completionSelectors)
            {
                var element = await page.QuerySelectorAsync(selector);
                if (element != null)
                {
                    _logger.LogInformation("Course appears to be complete (found completion element)");
                    return true;
                }
            }

            _logger.LogInformation("Course is not complete");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking course completion status");
            return false;
        }
    }

    private bool IsComplete(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        // Check for 100% completion in various formats
        // Match patterns like: "100%", "100 %", "100.0%", etc.
        var matches = PercentageRegex.Matches(text);
        
        bool foundPercentage = false;
        bool hasHundredPercent = false;
        
        foreach (Match match in matches)
        {
            if (double.TryParse(match.Groups[1].Value, out double percentage))
            {
                foundPercentage = true;
                
                // Check if we found 100% completion
                if (percentage >= 100.0)
                {
                    hasHundredPercent = true;
                    _logger.LogDebug("Found 100% completion indicator");
                }
            }
        }
        
        // If we found any percentage values, use that to determine completion
        // Only 100% indicates completion, anything else is incomplete
        if (foundPercentage)
        {
            if (hasHundredPercent)
            {
                _logger.LogDebug("Course is complete: found 100%");
                return true;
            }
            else
            {
                _logger.LogDebug("Course is incomplete: found percentage < 100%");
                return false;
            }
        }

        // If no percentage found, check for completion keywords in multiple languages
        var completionKeywords = new[]
        {
            "completed",    // English
            "complete",     // English
            "completato",   // Italian
            "completata",   // Italian (feminine)
            "terminato",    // Italian
            "terminata",    // Italian (feminine)
            "finito",       // Italian
            "finita"        // Italian (feminine)
        };

        foreach (var keyword in completionKeywords)
        {
            if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Course is complete: found keyword '{Keyword}'", keyword);
                return true;
            }
        }

        // If no percentage or completion keyword found, assume incomplete (to be safe and process it)
        return false;
    }
}
