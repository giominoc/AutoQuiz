using Microsoft.Extensions.Logging;
using AutoQuiz.App.Models;

namespace AutoQuiz.App.CopilotIntegration;

public class SpecGenerator
{
    private readonly ILogger<SpecGenerator> _logger;
    private readonly string _specsDir;

    public SpecGenerator(ILogger<SpecGenerator> logger)
    {
        _logger = logger;
        _specsDir = Path.Combine(Directory.GetCurrentDirectory(), "Specs");
        Directory.CreateDirectory(_specsDir);
    }

    public async Task<string> GenerateSpecFileAsync(QuizQuestion question, string copilotOutput)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var filename = $"quiz_q{question.QuestionNumber}_{timestamp}.spec.ts";
            var filepath = Path.Combine(_specsDir, filename);

            // Extract the actual test code from Copilot output
            var specContent = ExtractTestCode(copilotOutput);

            await File.WriteAllTextAsync(filepath, specContent);

            _logger.LogInformation("Generated spec file: {Path}", filepath);
            return filepath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating spec file");
            throw;
        }
    }

    private string ExtractTestCode(string output)
    {
        // Try to extract code blocks from markdown
        if (output.Contains("```"))
        {
            var startIndex = output.IndexOf("```");
            var endIndex = output.LastIndexOf("```");

            if (startIndex >= 0 && endIndex > startIndex)
            {
                var code = output.Substring(startIndex + 3, endIndex - startIndex - 3);
                
                // Remove language identifier if present
                if (code.StartsWith("typescript") || code.StartsWith("ts"))
                {
                    code = code.Substring(code.IndexOf('\n') + 1);
                }

                return code.Trim();
            }
        }

        // If no code blocks found, return as-is
        return output;
    }
}
