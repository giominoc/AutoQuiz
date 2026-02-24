using AutoQuiz.App.Models;
using AutoQuiz.App.Playwright;
using AutoQuiz.App.QuizDetection;
using AutoQuiz.App.CopilotIntegration;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace AutoQuiz.App.Services;

public class QuizAutomationService
{
    private readonly ILogger<QuizAutomationService> _logger;
    private readonly BrowserManager _browserManager;
    private readonly PageNavigator _pageNavigator;
    private readonly VideoSkipper _videoSkipper;
    private readonly QuizDetector _quizDetector;
    private readonly PromptGenerator _promptGenerator;
    private readonly CopilotCliExecutor _copilotExecutor;
    private readonly SpecGenerator _specGenerator;
    private readonly SpecExecutor _specExecutor;
    private readonly LoggerService _loggerService;
    private readonly LoginService _loginService;
    private readonly CourseNavigator _courseNavigator;

    public QuizAutomationService(
        ILogger<QuizAutomationService> logger,
        BrowserManager browserManager,
        PageNavigator pageNavigator,
        VideoSkipper videoSkipper,
        QuizDetector quizDetector,
        PromptGenerator promptGenerator,
        CopilotCliExecutor copilotExecutor,
        SpecGenerator specGenerator,
        SpecExecutor specExecutor,
        LoggerService loggerService,
        LoginService loginService,
        CourseNavigator courseNavigator)
    {
        _logger = logger;
        _browserManager = browserManager;
        _pageNavigator = pageNavigator;
        _videoSkipper = videoSkipper;
        _quizDetector = quizDetector;
        _promptGenerator = promptGenerator;
        _copilotExecutor = copilotExecutor;
        _specGenerator = specGenerator;
        _specExecutor = specExecutor;
        _loggerService = loggerService;
        _loginService = loginService;
        _courseNavigator = courseNavigator;
    }

    public async Task<QuizResult> RunAsync(AutomationConfig config)
    {
        _logger.LogInformation("Starting quiz automation");
        _loggerService.Log($"Starting quiz automation");

        IPage? page = null;
        var overallResult = new QuizResult();

        try
        {
            // Initialize browser
            page = await _browserManager.InitializeAsync(config.Headless);
            _loggerService.Log($"Browser initialized in {(config.Headless ? "headless" : "headed")} mode");

            // Navigate to course URL for login
            await _pageNavigator.NavigateToAsync(page, config.CourseUrl);
            _loggerService.Log($"Navigated to: {config.CourseUrl}");

            // Perform login
            _logger.LogInformation("Attempting login...");
            _loggerService.Log("🔐 Attempting login...");
            
            var loginSuccess = await _loginService.LoginAsync(page, config.Username, config.Password);
            
            if (!loginSuccess)
            {
                throw new Exception("Login failed. Please verify your credentials and ensure the login page is accessible. Check the browser for any error messages or CAPTCHAs that may require manual intervention.");
            }

            _logger.LogInformation("Login successful");
            _loggerService.Log("✅ Login successful");

            // Get list of incomplete courses
            var incompleteCourses = await _courseNavigator.GetIncompleteCoursesAsync(page);
            
            if (incompleteCourses.Count == 0)
            {
                _logger.LogInformation("No incomplete courses found, processing current URL");
                _loggerService.Log("No incomplete courses found, processing current URL");
                
                // Process the current course URL directly
                incompleteCourses.Add(config.CourseUrl);
            }
            else
            {
                _logger.LogInformation("Found {Count} incomplete courses", incompleteCourses.Count);
                _loggerService.Log($"📚 Found {incompleteCourses.Count} incomplete courses");
            }

            // Process each incomplete course
            for (int i = 0; i < incompleteCourses.Count; i++)
            {
                var courseUrl = incompleteCourses[i];
                _logger.LogInformation("Processing course {Index}/{Total}: {Url}", i + 1, incompleteCourses.Count, courseUrl);
                _loggerService.Log($"📖 Processing course {i + 1}/{incompleteCourses.Count}: {courseUrl}");

                // Navigate to the course
                var navSuccess = await _courseNavigator.NavigateToCourseAsync(page, courseUrl);
                
                if (!navSuccess)
                {
                    _logger.LogWarning("Failed to navigate to course, skipping");
                    _loggerService.Log("⚠️  Failed to navigate to course, skipping");
                    continue;
                }

                // Process the course with retries
                var courseResult = await ProcessCourseWithRetriesAsync(page, config);
                
                // Aggregate results
                overallResult.TotalQuestions += courseResult.TotalQuestions;
                overallResult.CorrectAnswers += courseResult.CorrectAnswers;
                overallResult.IncorrectQuestions.AddRange(courseResult.IncorrectQuestions);

                _logger.LogInformation("Course completed with score: {Score}%", courseResult.ScorePercentage);
                _loggerService.Log($"Course completed with score: {courseResult.ScorePercentage:F1}%");
            }

            _logger.LogInformation("All courses processed");
            _loggerService.Log("✅ All courses processed");

            return overallResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during quiz automation");
            _loggerService.Log($"❌ Error: {ex.Message}");
            throw;
        }
    }

    private async Task<QuizResult> ProcessCourseWithRetriesAsync(IPage page, AutomationConfig config)
    {
        int retryCount = 0;
        QuizResult? result = null;

        while (retryCount < config.MaxRetries)
        {
            retryCount++;
            _logger.LogInformation("Attempt #{Retry}", retryCount);
            _loggerService.Log($"=== ATTEMPT #{retryCount} ===");

            result = await ProcessCourseAsync(page, config);

            if (result.IsPerfectScore)
            {
                _logger.LogInformation("Perfect score achieved! 🎉");
                _loggerService.Log("✅ Perfect score achieved! 100% correct!");
                break;
            }

            if (retryCount < config.MaxRetries)
            {
                _logger.LogInformation("Score: {Score}%, retrying...", result.ScorePercentage);
                _loggerService.Log($"Score: {result.ScorePercentage:F1}%, retrying...");
                
                // Restart quiz
                await RestartQuizAsync(page);
                await Task.Delay(2000);
            }
        }

        if (result != null && !result.IsPerfectScore)
        {
            _logger.LogWarning("Maximum retries reached. Final score: {Score}%", result.ScorePercentage);
            _loggerService.Log($"⚠️  Maximum retries reached. Final score: {result.ScorePercentage:F1}%");
        }

        return result ?? new QuizResult();
    }

    private async Task<QuizResult> ProcessCourseAsync(IPage page, AutomationConfig config)
    {
        var result = new QuizResult();
        var questionNumber = 0;

        while (true)
        {
            // Skip videos
            await _videoSkipper.SkipVideoIfPresentAsync(page, config.VideoSkipDelayMs);

            // Check for quiz
            if (await _quizDetector.IsQuizPresentAsync(page))
            {
                _logger.LogInformation("Quiz detected!");
                _loggerService.Log("📝 Quiz detected!");

                // Extract questions
                var questions = await _quizDetector.ExtractQuestionsAsync(page);

                foreach (var question in questions)
                {
                    questionNumber++;
                    _logger.LogInformation("Processing question #{Number}: {Question}", 
                        questionNumber, question.QuestionText);
                    _loggerService.Log($"Question #{questionNumber}: {question.QuestionText}");

                    // Generate prompt
                    var prompt = _promptGenerator.GeneratePrompt(question);

                    // Get Copilot response
                    var copilotOutput = await _copilotExecutor.ExecuteAsync(prompt);
                    _loggerService.Log("Received Copilot response");

                    // Generate spec file
                    var specPath = await _specGenerator.GenerateSpecFileAsync(question, copilotOutput);
                    _loggerService.Log($"Generated spec: {Path.GetFileName(specPath)}");

                    // Execute spec
                    var success = await _specExecutor.ExecuteSpecAsync(specPath);
                    
                    if (success)
                    {
                        result.CorrectAnswers++;
                        _loggerService.Log("✅ Answer submitted");
                    }
                    else
                    {
                        result.IncorrectQuestions.Add(question.QuestionText);
                        _loggerService.Log("❌ Answer may be incorrect");
                    }

                    result.TotalQuestions++;
                }
            }

            // Try to go to next page
            var hasNext = await _pageNavigator.TryClickNextAsync(page);
            if (!hasNext)
            {
                _logger.LogInformation("No more pages, course completed");
                _loggerService.Log("Course navigation completed");
                break;
            }

            await Task.Delay(2000); // Wait between pages
        }

        return result;
    }

    private async Task RestartQuizAsync(IPage page)
    {
        _logger.LogInformation("Restarting quiz...");
        _loggerService.Log("🔄 Restarting quiz...");

        try
        {
            // Try to find restart/retake button
            var restartButtons = new[]
            {
                "button:has-text('Restart')",
                "button:has-text('Retake')",
                "button:has-text('Try Again')",
                "[data-purpose='restart-quiz']"
            };

            foreach (var button in restartButtons)
            {
                var element = await page.QuerySelectorAsync(button);
                if (element != null)
                {
                    await element.ClickAsync();
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                    _logger.LogInformation("Quiz restarted");
                    return;
                }
            }

            // If no restart button, reload the page
            await page.ReloadAsync();
            _logger.LogInformation("Page reloaded");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error restarting quiz");
        }
    }
}
