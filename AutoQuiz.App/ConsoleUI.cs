using AutoQuiz.App.Models;

namespace AutoQuiz.App;

public class ConsoleUI
{
    public static void ShowHeader()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                      AutoQuiz                                  ║");
        Console.WriteLine("║          Automated Quiz Solver for Video Courses               ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
    }

    public static AutomationConfig GetConfiguration()
    {
        var config = new AutomationConfig();

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("📋 Configuration");
        Console.ResetColor();
        Console.WriteLine();

        // Get Course URL
        Console.Write("Enter Course URL: ");
        config.CourseUrl = Console.ReadLine()?.Trim() ?? string.Empty;

        while (string.IsNullOrWhiteSpace(config.CourseUrl) || !Uri.IsWellFormedUriString(config.CourseUrl, UriKind.Absolute))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Invalid URL. Please enter a valid URL.");
            Console.ResetColor();
            Console.Write("Enter Course URL: ");
            config.CourseUrl = Console.ReadLine()?.Trim() ?? string.Empty;
        }

        // Get Username
        Console.WriteLine();
        Console.Write("Enter Username: ");
        config.Username = Console.ReadLine()?.Trim() ?? string.Empty;

        while (string.IsNullOrWhiteSpace(config.Username))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Username cannot be empty.");
            Console.ResetColor();
            Console.Write("Enter Username: ");
            config.Username = Console.ReadLine()?.Trim() ?? string.Empty;
        }

        // Get Password
        Console.WriteLine();
        Console.Write("Enter Password: ");
        config.Password = ReadPassword();

        while (string.IsNullOrWhiteSpace(config.Password))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Password cannot be empty.");
            Console.ResetColor();
            Console.Write("Enter Password: ");
            config.Password = ReadPassword();
        }

        // Get Browser Mode
        Console.WriteLine();
        Console.WriteLine("Browser Mode:");
        Console.WriteLine("  1. Headless (faster, runs in background)");
        Console.WriteLine("  2. Headed (visible browser window)");
        Console.Write("Select mode (1 or 2): ");
        var modeInput = Console.ReadLine()?.Trim();

        config.Headless = modeInput != "2";

        // Max Retries
        Console.WriteLine();
        Console.Write("Maximum retries (default 3): ");
        var retriesInput = Console.ReadLine()?.Trim();
        if (!string.IsNullOrWhiteSpace(retriesInput) && int.TryParse(retriesInput, out var retries))
        {
            config.MaxRetries = retries;
        }

        Console.WriteLine();
        return config;
    }

    public static void ShowConfiguration(AutomationConfig config)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✅ Configuration Summary:");
        Console.ResetColor();
        Console.WriteLine($"  Course URL: {config.CourseUrl}");
        Console.WriteLine($"  Username: {config.Username}");
        Console.WriteLine($"  Password: {new string('*', config.Password.Length)}");
        Console.WriteLine($"  Browser Mode: {(config.Headless ? "Headless" : "Headed")}");
        Console.WriteLine($"  Max Retries: {config.MaxRetries}");
        Console.WriteLine();
    }

    public static bool ConfirmStart()
    {
        Console.Write("Start automation? (y/n): ");
        var response = Console.ReadLine()?.Trim().ToLower();
        return response == "y" || response == "yes";
    }

    public static void ShowProgress(string message)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
    }

    public static void ShowResult(QuizResult result)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                        RESULTS                                 ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();

        Console.WriteLine($"Total Questions: {result.TotalQuestions}");
        Console.WriteLine($"Correct Answers: {result.CorrectAnswers}");
        
        if (result.IsPerfectScore)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Score: {result.ScorePercentage:F1}% 🎉");
            Console.WriteLine("✅ PERFECT SCORE!");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Score: {result.ScorePercentage:F1}%");
        }
        Console.ResetColor();

        if (result.IncorrectQuestions.Any())
        {
            Console.WriteLine();
            Console.WriteLine("Incorrect Questions:");
            foreach (var q in result.IncorrectQuestions)
            {
                Console.WriteLine($"  ❌ {q}");
            }
        }

        Console.WriteLine();
    }

    public static void ShowError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"❌ Error: {message}");
        Console.ResetColor();
    }

    public static void WaitForExit()
    {
        try
        {
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            if (!Console.IsInputRedirected)
            {
                Console.ReadKey();
            }
        }
        catch
        {
            // Ignore errors in non-interactive environments
        }
    }

    private static string ReadPassword()
    {
        var password = string.Empty;
        ConsoleKey key;

        do
        {
            var keyInfo = Console.ReadKey(intercept: true);
            key = keyInfo.Key;

            if (key == ConsoleKey.Backspace && password.Length > 0)
            {
                password = password[0..^1];
                Console.Write("\b \b");
            }
            else if (!char.IsControl(keyInfo.KeyChar))
            {
                password += keyInfo.KeyChar;
                Console.Write("*");
            }
        } while (key != ConsoleKey.Enter);

        Console.WriteLine();
        return password;
    }
}
