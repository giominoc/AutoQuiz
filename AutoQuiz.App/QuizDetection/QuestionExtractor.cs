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
                "p:has-text('?')"
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
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting questions");
        }

        return questions;
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
                "[class*='answer']"
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
                        break;
                }
            }

            // If no answers found, try to get all labels
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
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting answers");
        }

        return answers.Distinct().ToList();
    }
}
