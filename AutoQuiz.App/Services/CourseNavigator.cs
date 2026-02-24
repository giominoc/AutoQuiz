using Microsoft.Playwright;
using Microsoft.Extensions.Logging;

namespace AutoQuiz.App.Services;

public class CourseNavigator
{
    private readonly ILogger<CourseNavigator> _logger;

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
                            if (text != null && !text.Contains("Completed") && !text.Contains("100%"))
                            {
                                var fullUrl = href.StartsWith("http") ? href : new Uri(new Uri(page.Url), href).ToString();
                                
                                if (!incompleteCourses.Contains(fullUrl))
                                {
                                    incompleteCourses.Add(fullUrl);
                                    _logger.LogInformation("Found incomplete course: {Url}", fullUrl);
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
            // Check for completion indicators
            var completionSelectors = new[]
            {
                ":has-text('Completed')",
                ":has-text('100%')",
                ":has-text('Certificate')",
                "[data-purpose='completion-indicator']"
            };

            foreach (var selector in completionSelectors)
            {
                var element = await page.QuerySelectorAsync(selector);
                if (element != null)
                {
                    _logger.LogInformation("Course appears to be complete");
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
}
