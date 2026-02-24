namespace AutoQuiz.App.Models;

public class QuizQuestion
{
    public string QuestionText { get; set; } = string.Empty;
    public List<string> Answers { get; set; } = new();
    public string? ScreenshotPath { get; set; }
    public string? PageContext { get; set; }
    public int QuestionNumber { get; set; }
}
