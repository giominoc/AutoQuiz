using AutoQuiz.App.Models;
using Microsoft.Extensions.Logging;

namespace AutoQuiz.App.CopilotIntegration;

public class PromptGenerator
{
    private readonly ILogger<PromptGenerator> _logger;

    public PromptGenerator(ILogger<PromptGenerator> logger)
    {
        _logger = logger;
    }

    public string GeneratePrompt(QuizQuestion question, int attemptNumber = 1)
    {
        _logger.LogInformation("Generating prompt for question {Number}, attempt {Attempt}", 
            question.QuestionNumber, attemptNumber);

        var prompt = $@"Create a Playwright test that selects the correct answer for this quiz question:

Question: {question.QuestionText}

Available Answers:
{string.Join("\n", question.Answers.Select((a, i) => $"{i + 1}. {a}"))}

Page Context: {question.PageContext ?? "N/A"}

Requirements:
1. Analyze the question and determine the correct answer
2. Generate a Playwright test (.spec.ts) that:
   - Waits for the question to be visible
   - Selects the correct answer by clicking the appropriate radio button or checkbox
   - Clicks the submit or next button
3. Use appropriate Playwright selectors (data-purpose, text content, etc.)
4. Include proper waits and error handling
5. The test should be ready to execute

{(attemptNumber > 1 ? $"Note: This is attempt #{attemptNumber}. Previous attempts failed. Please reconsider the correct answer." : "")}

Generate only the Playwright test code, no explanation needed.";

        return prompt;
    }

    public string GenerateRetryPrompt(QuizQuestion question, int attemptNumber, List<string> previouslyTriedAnswers)
    {
        _logger.LogInformation("Generating retry prompt for question {Number}, attempt {Attempt}", 
            question.QuestionNumber, attemptNumber);

        var availableAnswers = question.Answers.Where(a => !previouslyTriedAnswers.Contains(a)).ToList();

        var prompt = $@"Create a Playwright test for a quiz question (RETRY ATTEMPT #{attemptNumber}):

Question: {question.QuestionText}

Available Answers (excluding previously tried):
{string.Join("\n", availableAnswers.Select((a, i) => $"{i + 1}. {a}"))}

Previously Tried (INCORRECT):
{string.Join("\n", previouslyTriedAnswers.Select((a, i) => $"❌ {a}"))}

Requirements:
1. DO NOT select any of the previously tried answers
2. Choose from the remaining available answers
3. Generate a Playwright test that selects a DIFFERENT answer
4. Use appropriate selectors and waits

Generate only the Playwright test code.";

        return prompt;
    }
}
