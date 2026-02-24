using AutoQuiz.App;
using AutoQuiz.App.Services;
using AutoQuiz.App.Playwright;
using AutoQuiz.App.QuizDetection;
using AutoQuiz.App.CopilotIntegration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Setup Dependency Injection
var serviceProvider = new ServiceCollection()
    .AddLogging(builder =>
    {
        builder.AddConsole();
        builder.SetMinimumLevel(LogLevel.Information);
    })
    // Browser & Navigation
    .AddSingleton<BrowserManager>()
    .AddSingleton<PageNavigator>()
    .AddSingleton<VideoSkipper>()
    // Quiz Detection
    .AddSingleton<ScreenshotCapture>()
    .AddSingleton<QuestionExtractor>()
    .AddSingleton<QuizDetector>()
    // Copilot Integration
    .AddSingleton<PromptGenerator>()
    .AddSingleton<CopilotCliExecutor>()
    .AddSingleton<SpecGenerator>()
    .AddSingleton<SpecExecutor>()
    // Services
    .AddSingleton<LoggerService>()
    .AddSingleton<LoginService>()
    .AddSingleton<CourseNavigator>()
    .AddSingleton<QuizAutomationService>()
    .BuildServiceProvider();

try
{
    // Show header
    ConsoleUI.ShowHeader();

    // Get configuration from user
    var config = ConsoleUI.GetConfiguration();
    ConsoleUI.ShowConfiguration(config);

    // Confirm start
    if (!ConsoleUI.ConfirmStart())
    {
        Console.WriteLine("Operation cancelled.");
        return;
    }

    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("🚀 Starting automation...");
    Console.ResetColor();
    Console.WriteLine();

    // Run automation
    var automationService = serviceProvider.GetRequiredService<QuizAutomationService>();
    var result = await automationService.RunAsync(config);

    // Show results
    ConsoleUI.ShowResult(result);

    // Get log file path
    var loggerService = serviceProvider.GetRequiredService<LoggerService>();
    Console.WriteLine($"📄 Detailed logs saved to: {loggerService.GetLogFilePath()}");
}
catch (Exception ex)
{
    ConsoleUI.ShowError(ex.Message);
    Console.WriteLine();
    Console.WriteLine("Stack trace:");
    Console.WriteLine(ex.StackTrace);
}
finally
{
    // Cleanup
    var browserManager = serviceProvider.GetService<BrowserManager>();
    browserManager?.Dispose();

    ConsoleUI.WaitForExit();
}
