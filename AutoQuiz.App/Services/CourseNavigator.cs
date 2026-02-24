using Microsoft.Playwright;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace AutoQuiz.App.Services;

public class CourseNavigator
{
    private readonly ILogger<CourseNavigator> _logger;
    private static readonly Regex PercentageRegex = new Regex(@"(\d+(?:\.\d+)?)\s*%", RegexOptions.Compiled);
    private const double MaxPercentage = 100.0;

    // Novolearn-specific allowed course titles (exact matches with flexible whitespace)
    private static readonly string[] AllowedCourseTitles = new[]
    {
        "Anti-Corruption 2.0 INT",
        "FORMAZIONE IN MATERIA DI D.LGS.",  // Covers variations like "FORMAZIONE IN MATERIA DI D.LGS. 231/2001"
        "GDPR - REFRESHER 3 INT"
    };

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
                            // First check if this is an allowed course title
                            var courseTitle = text?.Trim() ?? "";
                            if (!IsAllowedCourseTitle(courseTitle))
                            {
                                _logger.LogDebug("Skipping course '{Title}' - not in allowed list", courseTitle);
                                continue;
                            }

                            _logger.LogInformation("Found allowed course title: '{Title}'", courseTitle);

                            // Get parent element to check for completion percentage in container
                            var parent = await element.EvaluateHandleAsync("el => el.closest('div, li, tr') || el.parentElement");
                            string? parentText = null;
                            
                            if (parent != null)
                            {
                                try
                                {
                                    var parentElement = parent.AsElement();
                                    if (parentElement != null)
                                    {
                                        parentText = await parentElement.TextContentAsync();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // Ignore if we can't get parent text - element may not be valid
                                    _logger.LogDebug(ex, "Could not get parent element text");
                                }
                            }
                            
                            // Use parent text if available, otherwise use element text
                            var checkText = !string.IsNullOrEmpty(parentText) ? parentText : text;
                            
                            // Check if it's not completed and at 0% progress
                            if (checkText != null && !IsComplete(checkText) && IsZeroProgress(checkText))
                            {
                                var fullUrl = href.StartsWith("http") ? href : new Uri(new Uri(page.Url), href).ToString();
                                
                                if (!incompleteCourses.Contains(fullUrl))
                                {
                                    incompleteCourses.Add(fullUrl);
                                    _logger.LogInformation("Found incomplete course at 0%: {Url} (Title: {Title}, Progress: {Text})", fullUrl, courseTitle, checkText?.Trim());
                                }
                            }
                            else
                            {
                                _logger.LogDebug("Skipping course '{Title}' - already completed or has progress > 0%", courseTitle);
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

    private bool IsAllowedCourseTitle(string courseTitle)
    {
        if (string.IsNullOrWhiteSpace(courseTitle))
        {
            return false;
        }

        // Normalize whitespace for comparison
        var normalizedTitle = Regex.Replace(courseTitle.Trim(), @"\s+", " ");

        foreach (var allowedTitle in AllowedCourseTitles)
        {
            var normalizedAllowed = Regex.Replace(allowedTitle, @"\s+", " ");
            
            // Check if the course title starts with the allowed pattern (for variations like "FORMAZIONE IN MATERIA DI D.LGS. 231/2001")
            // Note: StartsWith also handles exact matches (a string always starts with itself)
            if (normalizedTitle.StartsWith(normalizedAllowed, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Course title '{Title}' matches allowed pattern '{Pattern}'", courseTitle, allowedTitle);
                return true;
            }
        }

        _logger.LogDebug("Course title '{Title}' is not in the allowed list", courseTitle);
        return false;
    }

    private bool IsZeroProgress(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return true; // No progress indicator means 0%
        }

        // Check for green "COMPLETATO" badge or completion indicators (these should be excluded)
        var completionBadges = new[]
        {
            "COMPLETATO",       // Italian completed badge
            "COMPLETATA",       // Italian completed (feminine)
            "COMPLETED",        // English completed badge
            "✓",                // Checkmark
            "✔"                 // Checkmark variant
        };

        foreach (var badge in completionBadges)
        {
            if (text.Contains(badge, StringComparison.OrdinalIgnoreCase))
            {
                // However, if we see "0% COMPLETATO", that's actually 0% progress, not completed
                // Check if there's a "0%" before the badge
                var percentageMatches = PercentageRegex.Matches(text);
                bool hasZeroPercent = false;
                
                foreach (Match match in percentageMatches)
                {
                    if (double.TryParse(match.Groups[1].Value, out double percentage))
                    {
                        if (percentage == 0)
                        {
                            hasZeroPercent = true;
                            break;
                        }
                    }
                }

                if (!hasZeroPercent)
                {
                    _logger.LogDebug("Course has completion badge '{Badge}', excluding", badge);
                    return false;
                }
            }
        }

        // Check for percentage indicators
        var matches = PercentageRegex.Matches(text);
        
        foreach (Match match in matches)
        {
            if (double.TryParse(match.Groups[1].Value, out double percentage))
            {
                // Only 0% is acceptable - any percentage > 0 means the course has been started
                if (percentage > 0 && percentage < MaxPercentage)
                {
                    _logger.LogDebug("Course has progress: {Percentage}%", percentage);
                    return false;
                }
                // If we found 0%, that's good - continue checking
                if (percentage == 0)
                {
                    _logger.LogDebug("Course is at 0% progress");
                    // Continue checking for other indicators
                }
            }
        }

        // Check for "In Progress" or similar indicators
        var progressIndicators = new[]
        {
            "in progress",
            "in corso",        // Italian
            "continua",        // Italian - continue
            "riprendi",        // Italian - resume
            "resume"           // English
        };

        foreach (var indicator in progressIndicators)
        {
            if (text.Contains(indicator, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Course has been started: found indicator '{Indicator}'", indicator);
                return false;
            }
        }

        // If no percentage or progress indicator found, assume 0%
        return true;
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
                if (percentage >= MaxPercentage)
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
