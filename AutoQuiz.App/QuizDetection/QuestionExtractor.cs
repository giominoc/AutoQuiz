using Microsoft.Playwright;
using Microsoft.Extensions.Logging;
using AutoQuiz.App.Models;

namespace AutoQuiz.App.QuizDetection;

public class QuestionExtractor
{
    private readonly ILogger<QuestionExtractor> _logger;
    private readonly ScreenshotCapture _screenshotCapture;

    public QuestionExtractor(ILogger<QuestionExtractor> logger, ScreenshotCapture screenshotCapture)
    {
        _logger = logger;
        _screenshotCapture = screenshotCapture;
    }

    public async Task<List<QuizQuestion>> ExtractAsync(IPage page)
    {
        var questions = new List<QuizQuestion>();

        try
        {
            // Try to extract question text
            var questionSelectors = new[]
            {
                "[data-purpose='question-title']",
                ".question-text",
                "[class*='question']",
                "h2:has-text('Question')",
                "h2:has-text('Domanda')",
                "h3:has-text('Question')",
                "h3:has-text('Domanda')",
                "p:has-text('?')",
                
                // SCORM specific
                "[class*='quiz-question']",
                "[id*='question']",
                "[class*='domanda']",
                ".scorm-question"
            };

            string questionText = string.Empty;
            foreach (var selector in questionSelectors)
            {
                var element = await page.QuerySelectorAsync(selector);
                if (element != null)
                {
                    questionText = await element.TextContentAsync() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(questionText))
                        break;
                }
            }

            // If no question found via selectors, try to get first paragraph with "?"
            if (string.IsNullOrWhiteSpace(questionText))
            {
                var paragraphs = await page.QuerySelectorAllAsync("p");
                foreach (var p in paragraphs)
                {
                    var text = await p.TextContentAsync();
                    if (text != null && text.Contains("?"))
                    {
                        questionText = text;
                        break;
                    }
                }
            }

            // Extract answers
            var answers = await ExtractAnswersAsync(page);

            if (!string.IsNullOrWhiteSpace(questionText) && answers.Any())
            {
                var question = new QuizQuestion
                {
                    QuestionText = questionText.Trim(),
                    Answers = answers,
                    QuestionNumber = questions.Count + 1,
                    PageContext = await page.TitleAsync()
                };

                // Capture screenshot
                question.ScreenshotPath = await _screenshotCapture.CaptureAsync(page, question.QuestionNumber);

                questions.Add(question);
                _logger.LogInformation("Extracted question: {Question} with {Count} answers", 
                    questionText.Substring(0, Math.Min(50, questionText.Length)), answers.Count);
            }
            else
            {
                _logger.LogWarning("Could not extract complete question data");
                
                // Capture diagnostic artifacts
                await CaptureDiagnosticsAsync(page, questionText, answers);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting questions");
            
            // Capture diagnostics on exception
            try
            {
                await CaptureDiagnosticsAsync(page, string.Empty, new List<string>());
            }
            catch { /* Ignore diagnostic errors */ }
        }

        return questions;
    }

    private async Task CaptureDiagnosticsAsync(IPage page, string questionText, List<string> answers)
    {
        try
        {
            _logger.LogInformation("Capturing diagnostic artifacts for failed extraction...");
            
            // Capture screenshot
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var screenshotPath = Path.Combine("Logs", "Screenshots", $"diagnostic_{timestamp}.png");
            Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath)!);
            await page.ScreenshotAsync(new() { Path = screenshotPath, FullPage = true });
            _logger.LogInformation("Diagnostic screenshot saved: {Path}", screenshotPath);

            // Dump relevant HTML
            var bodyHtml = await page.InnerHTMLAsync("body");
            var htmlPath = Path.Combine("Logs", $"diagnostic_{timestamp}.html");
            await File.WriteAllTextAsync(htmlPath, bodyHtml);
            _logger.LogInformation("HTML dump saved: {Path}", htmlPath);
            
            _logger.LogInformation("Diagnostic info - Question text: '{Text}', Answers found: {Count}", 
                string.IsNullOrWhiteSpace(questionText) ? "(none)" : questionText.Substring(0, Math.Min(100, questionText.Length)), 
                answers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing diagnostics");
        }
    }

    private async Task<List<string>> ExtractAnswersAsync(IPage page)
    {
        var answers = new List<string>();

        try
        {
            // Try different answer selectors
            var answerSelectors = new[]
            {
                "input[type='radio'] + label",
                "input[type='checkbox'] + label",
                "[data-purpose='answer-label']",
                ".answer-text",
                "[class*='answer']",
                
                // SCORM specific selectors
                "[role='radio']",
                "[role='checkbox']",
                "[class*='choice']",
                "[class*='option']",
                "li[class*='answer']",
                "div[class*='answer']",
                "button[class*='answer']",
                
                // Italian specific
                "[class*='risposta']",
                "li[class*='risposta']"
            };

            foreach (var selector in answerSelectors)
            {
                var elements = await page.QuerySelectorAllAsync(selector);
                if (elements.Any())
                {
                    foreach (var element in elements)
                    {
                        var text = await element.TextContentAsync();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            answers.Add(text.Trim());
                        }
                    }

                    if (answers.Any())
                    {
                        _logger.LogInformation("Found {Count} answers using selector: {Selector}", answers.Count, selector);
                        break;
                    }
                }
            }

            // If no answers found, try to get clickable list items
            if (!answers.Any())
            {
                var listItems = await page.QuerySelectorAllAsync("ul li, ol li");
                foreach (var item in listItems)
                {
                    var text = await item.TextContentAsync();
                    if (!string.IsNullOrWhiteSpace(text) && text.Length < 500)
                    {
                        // Check if the item looks like an answer option
                        var trimmedText = text.Trim();
                        if (trimmedText.Length > 0 && !trimmedText.Contains('\n'))
                        {
                            answers.Add(trimmedText);
                        }
                    }
                }
                
                if (answers.Any())
                {
                    _logger.LogInformation("Found {Count} answers from list items", answers.Count);
                }
            }

            // If still no answers found, try all labels as last resort
            if (!answers.Any())
            {
                var labels = await page.QuerySelectorAllAsync("label");
                foreach (var label in labels)
                {
                    var text = await label.TextContentAsync();
                    if (!string.IsNullOrWhiteSpace(text) && text.Length < 500)
                    {
                        answers.Add(text.Trim());
                    }
                }
                
                if (answers.Any())
                {
                    _logger.LogInformation("Found {Count} answers from labels", answers.Count);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting answers");
        }

        return answers.Distinct().ToList();
    }
}
