# AutoQuiz - Automated Quiz Solver for Video Courses

AutoQuiz is a C# desktop application that automates the completion of quiz tests in video courses using Playwright for browser automation and GitHub Copilot CLI for solving quiz questions.

## Features

- 🎯 **Automated Quiz Solving**: Automatically detects and solves quiz questions in video courses
- 🎬 **Video Skipping**: Automatically skips or fast-forwards through video content
- 🤖 **AI-Powered**: Uses GitHub Copilot CLI to determine correct answers
- 🔄 **Retry Logic**: Automatically retries until achieving 100% score
- 📝 **Comprehensive Logging**: Logs all questions, answers, and results
- 🖼️ **Screenshot Capture**: Captures screenshots of quiz questions for reference
- 🎮 **Flexible Browser Modes**: Supports both headless and headed browser modes

## Architecture

The project is organized into modular components:

```
AutoQuiz.App/
├── Models/              # Data models (QuizQuestion, QuizResult, AutomationConfig)
├── Playwright/          # Browser automation components
│   ├── BrowserManager   # Browser initialization and management
│   ├── VideoSkipper     # Video detection and skipping
│   └── PageNavigator    # Page navigation logic
├── QuizDetection/       # Quiz detection and extraction
│   ├── QuizDetector     # Detects quiz presence
│   ├── QuestionExtractor # Extracts questions and answers
│   └── ScreenshotCapture # Captures screenshots
├── CopilotIntegration/  # GitHub Copilot CLI integration
│   ├── PromptGenerator  # Generates prompts for Copilot
│   ├── CopilotCliExecutor # Executes Copilot CLI commands
│   ├── SpecGenerator    # Generates Playwright spec files
│   └── SpecExecutor     # Executes generated specs
├── Services/            # Main orchestration services
│   ├── QuizAutomationService # Main workflow orchestration
│   └── LoggerService    # Logging service
├── Logs/                # Execution logs and screenshots
└── Specs/               # Generated Playwright test specs
```

## Prerequisites

- .NET 10.0 or higher
- Microsoft Playwright for .NET
- GitHub Copilot CLI (optional, will use mock responses if not available)
- Chromium browser (installed automatically by Playwright)

## Installation

1. **Clone the repository:**
   ```bash
   git clone https://github.com/giominoc/AutoQuiz.git
   cd AutoQuiz
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

3. **Install Playwright browsers:**
   ```bash
   cd AutoQuiz.App
   dotnet build
   dotnet playwright install chromium
   ```

4. **Optional: Install GitHub Copilot CLI**
   ```bash
   gh extension install github/gh-copilot
   ```

## Usage

1. **Run the application:**
   ```bash
   cd AutoQuiz.App
   dotnet run
   ```

2. **Follow the prompts:**
   - Enter the course URL
   - Select browser mode (Headless or Headed)
   - Configure maximum retries (default: 3)
   - Confirm to start automation

3. **Monitor progress:**
   - The application will display real-time progress
   - Logs are saved to the `Logs/` directory
   - Screenshots are saved to `Logs/Screenshots/`
   - Generated specs are saved to `Specs/`

## How It Works

1. **Browser Initialization**: Launches a Playwright browser session
2. **Navigation**: Navigates to the provided course URL
3. **Video Skipping**: Detects and skips video content automatically
4. **Quiz Detection**: Monitors the page for quiz questions
5. **Question Extraction**: Extracts question text and answer options
6. **AI Analysis**: Sends the question to GitHub Copilot CLI
7. **Spec Generation**: Generates a Playwright test to select the correct answer
8. **Execution**: Runs the generated test to submit the answer
9. **Retry Logic**: If score is not 100%, restarts and retries with improved prompts
10. **Results**: Reports final score and completion status

## Configuration

The application can be configured through the console UI:

- **Course URL**: The URL of the video course
- **Browser Mode**: 
  - Headless: Faster, runs in background
  - Headed: Visible browser window for debugging
- **Max Retries**: Number of retry attempts (default: 3)
- **Video Skip Delay**: Delay in milliseconds for video skipping (default: 2000)

## Logging

All activities are logged to:
- Console output (real-time)
- Log files in `Logs/` directory with timestamp
- Screenshots in `Logs/Screenshots/`
- Generated specs in `Specs/`

## Technical Stack

- **Language**: C# (.NET 10.0)
- **UI**: Console application (easily extensible to WPF/WinForms on Windows)
- **Browser Automation**: Microsoft Playwright for .NET
- **AI Integration**: GitHub Copilot CLI
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **Logging**: Microsoft.Extensions.Logging

## Limitations

- Currently supports quiz formats with radio buttons and checkboxes
- Video skipping depends on common video player implementations
- GitHub Copilot CLI is optional; mock responses used when unavailable
- Designed for English-language courses (extensible to other languages)

## Future Enhancements

- Support for more quiz question types (fill-in-the-blank, matching, etc.)
- Multi-language support
- WPF/WinForms GUI for Windows users
- Support for additional video course platforms
- Machine learning-based answer improvement
- Parallel processing for multiple courses
- Cloud deployment options

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is open source and available under the MIT License.

## Disclaimer

This tool is intended for educational purposes and personal learning automation. Please ensure you comply with the terms of service of any video course platform you use this tool with. The authors are not responsible for any misuse of this software.

## Support

For issues, questions, or contributions, please open an issue on GitHub:
https://github.com/giominoc/AutoQuiz/issues