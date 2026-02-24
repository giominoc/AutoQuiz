namespace AutoQuiz.App.Models;

public class QuizResult
{
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public double ScorePercentage => TotalQuestions > 0 ? (CorrectAnswers * 100.0) / TotalQuestions : 0;
    public bool IsPerfectScore => ScorePercentage == 100.0;
    public List<string> IncorrectQuestions { get; set; } = new();
}
