# AutoQuiz - Quick Start Examples

## Example 1: Basic Usage

```bash
# Navigate to the application directory
cd AutoQuiz.App

# Run the application
dotnet run

# When prompted, enter:
# - Course URL: https://example.com/course/introduction-to-programming
# - Browser Mode: 1 (Headless)
# - Max Retries: 3
# - Confirm: y
```

## Example 2: Headed Mode (Visual Debugging)

If you want to see the browser in action:

```bash
dotnet run

# When prompted:
# - Browser Mode: 2 (Headed)
```

## Example 3: Custom Configuration

```bash
dotnet run

# Configure as needed:
# - Course URL: Your course URL
# - Browser Mode: Your preference
# - Max Retries: 5 (or any number)
```

## What Happens During Execution

1. **Initialization**
   ```
   ╔════════════════════════════════════════════════════════════════╗
   ║                      AutoQuiz                                  ║
   ║          Automated Quiz Solver for Video Courses               ║
   ╚════════════════════════════════════════════════════════════════╝
   
   📋 Configuration
   Enter Course URL: https://example.com/course
   Browser Mode: 1 (Headless)
   Maximum retries: 3
   ```

2. **Automation Process**
   ```
   🚀 Starting automation...
   [10:30:15] Browser initialized in headless mode
   [10:30:16] Navigated to course URL
   [10:30:18] Checking for video to skip...
   [10:30:20] Quiz detected!
   [10:30:21] Question #1: What is the capital of France?
   [10:30:22] Received Copilot response
   [10:30:23] Generated spec: quiz_q1_20260224_103023.spec.ts
   [10:30:24] ✅ Answer submitted
   ```

3. **Results**
   ```
   ╔════════════════════════════════════════════════════════════════╗
   ║                        RESULTS                                 ║
   ╚════════════════════════════════════════════════════════════════╝
   
   Total Questions: 5
   Correct Answers: 5
   Score: 100.0% 🎉
   ✅ PERFECT SCORE!
   
   📄 Detailed logs saved to: Logs/quiz_automation_20260224_103015.log
   ```

## Output Files

After running the automation, you'll find:

```
AutoQuiz.App/
├── Logs/
│   ├── quiz_automation_20260224_103015.log    # Detailed execution log
│   └── Screenshots/
│       ├── question_1_20260224_103021.png     # Screenshot of question 1
│       ├── question_2_20260224_103045.png     # Screenshot of question 2
│       └── ...
└── Specs/
    ├── quiz_q1_20260224_103023.spec.ts        # Generated test for Q1
    ├── quiz_q2_20260224_103047.spec.ts        # Generated test for Q2
    └── ...
```

## Sample Log Output

```
[2026-02-24 10:30:15] Starting quiz automation for: https://example.com/course
[2026-02-24 10:30:15] Browser initialized in headless mode
[2026-02-24 10:30:16] Navigated to course URL
[2026-02-24 10:30:17] === ATTEMPT #1 ===
[2026-02-24 10:30:18] Checking for video to skip...
[2026-02-24 10:30:20] 📝 Quiz detected!
[2026-02-24 10:30:21] Question #1: What is the primary function of a compiler?
[2026-02-24 10:30:22] Received Copilot response
[2026-02-24 10:30:23] Generated spec: quiz_q1_20260224_103023.spec.ts
[2026-02-24 10:30:24] ✅ Answer submitted
[2026-02-24 10:30:25] Question #2: Which data structure uses LIFO?
[2026-02-24 10:30:26] Received Copilot response
[2026-02-24 10:30:27] Generated spec: quiz_q2_20260224_103027.spec.ts
[2026-02-24 10:30:28] ✅ Answer submitted
...
[2026-02-24 10:31:15] Course navigation completed
[2026-02-24 10:31:15] ✅ Perfect score achieved! 100% correct!
```

## Troubleshooting

### Issue: "GitHub Copilot CLI not available"
**Solution**: The application will use mock responses. To use real Copilot:
```bash
gh extension install github/gh-copilot
```

### Issue: "Browser not found"
**Solution**: Install Playwright browsers:
```bash
cd AutoQuiz.App
dotnet playwright install chromium
```

### Issue: "Cannot navigate to URL"
**Solution**: 
- Check your internet connection
- Verify the URL is accessible
- Try using headed mode (option 2) to see what's happening

### Issue: "Quiz not detected"
**Solution**:
- The quiz format may not be recognized
- Try running in headed mode to see the page
- Check the logs for more details
- The application may need customization for your specific course platform

## Advanced Usage

### Programmatic Usage

You can also use AutoQuiz programmatically:

```csharp
using AutoQuiz.App.Services;
using AutoQuiz.App.Models;

var config = new AutomationConfig
{
    CourseUrl = "https://example.com/course",
    Headless = true,
    MaxRetries = 3
};

// Set up services (see Program.cs for full DI setup)
var automationService = serviceProvider.GetRequiredService<QuizAutomationService>();
var result = await automationService.RunAsync(config);

Console.WriteLine($"Final Score: {result.ScorePercentage}%");
```

### Customizing Selectors

If the default selectors don't work for your platform, you can modify:
- `QuizDetection/QuizDetector.cs` - Quiz detection selectors
- `QuizDetection/QuestionExtractor.cs` - Question and answer selectors
- `Playwright/VideoSkipper.cs` - Video player selectors
- `Playwright/PageNavigator.cs` - Navigation button selectors

## Platform-Specific Tips

### Udemy
- Use the direct course URL
- May require login first (manually login before running)

### Coursera
- Navigate to the quiz page directly
- May need to adjust video skip timing

### EdX
- Works best with direct quiz URLs
- Some courses may require authentication

### Custom Platforms
- Start with headed mode to observe the page structure
- Check the generated screenshots in Logs/Screenshots/
- Modify selectors in the codebase as needed

## Performance Tips

1. **Headless Mode**: Use for faster execution
2. **Video Skip Delay**: Reduce to 1000ms for faster skipping
3. **Max Retries**: Set to 1 for testing, 3+ for production use
4. **Network**: Use a stable, fast internet connection

## Best Practices

1. Always test with a sample course first
2. Review generated specs for accuracy
3. Check logs after each run
4. Start with headed mode for unfamiliar platforms
5. Respect course platform terms of service
6. Use for personal learning and practice only
