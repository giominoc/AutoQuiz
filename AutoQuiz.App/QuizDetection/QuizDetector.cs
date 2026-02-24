using Microsoft.Playwright;
using Microsoft.Extensions.Logging;
using AutoQuiz.App.Models;

namespace AutoQuiz.App.QuizDetection;

public class QuizDetector
{
    private readonly ILogger<QuizDetector> _logger;
    private readonly QuestionExtractor _questionExtractor;

    public QuizDetector(ILogger<QuizDetector> logger, QuestionExtractor questionExtractor)
    {
        _logger = logger;
        _questionExtractor = questionExtractor;
    }

    public async Task<bool> IsQuizPresentAsync(IPage page)
    {
        try
        {
            _logger.LogInformation("Checking for quiz presence...");

            // Common quiz indicators
            var quizSelectors = new[]
            {
                "[data-purpose='quiz']",
                ".quiz-container",
                "[data-test='quiz']",
                "form[data-purpose='assessment']",
                "[role='group']:has(input[type='radio'])",
                "[class*='question']"
            };

            foreach (var selector in quizSelectors)
            {
                var element = await page.QuerySelectorAsync(selector);
                if (element != null)
                {
                    _logger.LogInformation("Quiz detected with selector: {Selector}", selector);
                    return true;
                }
            }

            // Check for text indicators
            var textContent = await page.TextContentAsync("body");
            var quizKeywords = new[] { "question", "quiz", "test", "assessment", "select the correct" };
            
            if (textContent != null && quizKeywords.Any(keyword => 
                textContent.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogInformation("Quiz detected by text content");
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error detecting quiz");
            return false;
        }
    }

    public async Task<List<QuizQuestion>> ExtractQuestionsAsync(IPage page)
    {
        _logger.LogInformation("Extracting quiz questions...");
        return await _questionExtractor.ExtractAsync(page);
    }
}
