namespace AutoQuiz.App.Models;

public class AutomationConfig
{
    public string CourseUrl { get; set; } = string.Empty;
    public bool Headless { get; set; }
    public int MaxRetries { get; set; } = 3;
    public int VideoSkipDelayMs { get; set; } = 2000;
}
