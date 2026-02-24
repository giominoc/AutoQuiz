using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AutoQuiz.App.CopilotIntegration;

public class CopilotCliExecutor
{
    private readonly ILogger<CopilotCliExecutor> _logger;

    public CopilotCliExecutor(ILogger<CopilotCliExecutor> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(string prompt)
    {
        _logger.LogInformation("Executing Copilot CLI with prompt length: {Length}", prompt.Length);

        try
        {
            // Check if GitHub Copilot CLI is available
            var checkProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "gh",
                    Arguments = "copilot --version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            checkProcess.Start();
            await checkProcess.WaitForExitAsync();

            if (checkProcess.ExitCode != 0)
            {
                _logger.LogWarning("GitHub Copilot CLI not available. Using mock response.");
                return GenerateMockPlaywrightSpec(prompt);
            }

            // Execute Copilot CLI
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "gh",
                    Arguments = $"copilot suggest -t shell",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            
            // Write prompt to stdin
            await process.StandardInput.WriteLineAsync(prompt);
            await process.StandardInput.FlushAsync();
            process.StandardInput.Close();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            
            await process.WaitForExitAsync();

            if (!string.IsNullOrWhiteSpace(error))
            {
                _logger.LogWarning("Copilot CLI stderr: {Error}", error);
            }

            _logger.LogInformation("Copilot CLI executed successfully");
            return output;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Copilot CLI");
            // Fallback to mock response
            return GenerateMockPlaywrightSpec(prompt);
        }
    }

    private string GenerateMockPlaywrightSpec(string prompt)
    {
        _logger.LogInformation("Generating mock Playwright spec");

        // This is a simplified mock that demonstrates the structure
        // In a real scenario, Copilot would analyze the question and generate appropriate test
        return @"
import { test, expect } from '@playwright/test';

test('answer quiz question', async ({ page }) => {
    // Wait for quiz to be visible
    await page.waitForSelector('[data-purpose=""quiz""], .quiz-container', { timeout: 5000 });
    
    // Select the first answer (mock - Copilot would determine correct answer)
    const answerSelector = 'input[type=""radio""]';
    await page.click(answerSelector);
    
    // Submit answer
    await page.click('button:has-text(""Submit""), button:has-text(""Next"")');
    
    // Wait for response
    await page.waitForLoadState('networkidle');
});
";
    }
}
