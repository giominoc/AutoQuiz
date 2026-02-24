# AutoQuiz Architecture Overview

## Project Structure

```
AutoQuiz/
│
├── AutoQuiz.App/                       # Main application project
│   │
│   ├── Models/                         # Data models
│   │   ├── AutomationConfig.cs        # Configuration settings
│   │   ├── QuizQuestion.cs            # Quiz question data
│   │   └── QuizResult.cs              # Quiz result data
│   │
│   ├── Playwright/                     # Browser automation
│   │   ├── BrowserManager.cs          # Browser lifecycle management
│   │   ├── VideoSkipper.cs            # Video detection and skipping
│   │   └── PageNavigator.cs           # Page navigation
│   │
│   ├── QuizDetection/                  # Quiz detection logic
│   │   ├── QuizDetector.cs            # Detects quiz presence
│   │   ├── QuestionExtractor.cs       # Extracts questions and answers
│   │   └── ScreenshotCapture.cs       # Captures screenshots
│   │
│   ├── CopilotIntegration/             # AI integration
│   │   ├── PromptGenerator.cs         # Generates AI prompts
│   │   ├── CopilotCliExecutor.cs      # Executes Copilot CLI
│   │   ├── SpecGenerator.cs           # Generates Playwright specs
│   │   └── SpecExecutor.cs            # Executes generated specs
│   │
│   ├── Services/                       # Core services
│   │   ├── QuizAutomationService.cs   # Main orchestration service
│   │   └── LoggerService.cs           # Logging service
│   │
│   ├── ConsoleUI.cs                    # Console user interface
│   ├── Program.cs                      # Application entry point
│   │
│   ├── Logs/                           # Runtime logs directory
│   └── Specs/                          # Generated specs directory
│
├── README.md                           # Main documentation
├── EXAMPLES.md                         # Usage examples
├── LICENSE                             # MIT License
├── .gitignore                          # Git ignore rules
└── AutoQuiz.slnx                       # Solution file
```

## Component Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         Program.cs                              │
│                  (Dependency Injection Setup)                   │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                        ConsoleUI                                │
│              (User Input & Output Display)                      │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                  QuizAutomationService                          │
│                 (Main Orchestration Logic)                      │
└──┬──────┬──────┬──────┬──────┬──────┬──────┬──────┬───────────┘
   │      │      │      │      │      │      │      │
   ▼      ▼      ▼      ▼      ▼      ▼      ▼      ▼
┌──────┐ ┌──┐ ┌────┐ ┌───┐ ┌───┐ ┌───┐ ┌───┐ ┌───────┐
│Browser│ │Page│ │Video│ │Quiz│ │Prompt│ │Copilot│ │Spec │ │Logger│
│Manager│ │Nav │ │Skip │ │Det │ │Gen  │ │Exec  │ │Gen  │ │Svc   │
└───────┘ └────┘ └─────┘ └────┘ └──────┘ └───────┘ └─────┘ └──────┘
```

## Data Flow

```
1. User Input
   ↓
2. Configuration Setup (AutomationConfig)
   ↓
3. Browser Initialization (BrowserManager)
   ↓
4. Navigate to Course URL (PageNavigator)
   ↓
5. Skip Videos (VideoSkipper)
   ↓
6. Detect Quiz (QuizDetector)
   ↓
7. Extract Questions (QuestionExtractor)
   ↓
8. Capture Screenshot (ScreenshotCapture)
   ↓
9. Generate Prompt (PromptGenerator)
   ↓
10. Execute Copilot CLI (CopilotCliExecutor)
   ↓
11. Generate Spec File (SpecGenerator)
   ↓
12. Execute Spec (SpecExecutor)
   ↓
13. Check Result (QuizResult)
   ↓
14. Retry if needed (back to step 6)
   ↓
15. Display Results (ConsoleUI)
```

## Key Design Patterns

### 1. Dependency Injection
- All services registered in Program.cs
- Constructor injection throughout
- Easy testing and modularity

### 2. Service Layer Pattern
- QuizAutomationService orchestrates workflow
- Each component has a single responsibility
- Loose coupling between components

### 3. Strategy Pattern
- Different detection strategies for quizzes
- Multiple selector strategies for elements
- Fallback mechanisms for Copilot CLI

### 4. Observer Pattern
- LoggerService logs all activities
- Console output for real-time feedback
- File output for detailed logs

## Technology Stack

| Component | Technology |
|-----------|------------|
| Language | C# (.NET 10.0) |
| UI | Console Application |
| Browser Automation | Microsoft Playwright |
| AI Integration | GitHub Copilot CLI |
| Dependency Injection | Microsoft.Extensions.DependencyInjection |
| Logging | Microsoft.Extensions.Logging |
| JSON Serialization | Newtonsoft.Json |

## Class Responsibilities

### Models
- **AutomationConfig**: Holds user configuration (URL, browser mode, retries)
- **QuizQuestion**: Represents a single quiz question with answers
- **QuizResult**: Contains quiz results and scoring information

### Playwright Components
- **BrowserManager**: Manages Playwright browser lifecycle
- **VideoSkipper**: Detects and skips video content
- **PageNavigator**: Handles page navigation and "Next" buttons

### QuizDetection Components
- **QuizDetector**: Detects presence of quiz on current page
- **QuestionExtractor**: Extracts question text and answer options
- **ScreenshotCapture**: Captures and saves screenshots

### CopilotIntegration Components
- **PromptGenerator**: Generates structured prompts for Copilot
- **CopilotCliExecutor**: Executes GitHub Copilot CLI commands
- **SpecGenerator**: Generates Playwright spec files from Copilot output
- **SpecExecutor**: Executes generated Playwright spec files

### Services
- **QuizAutomationService**: Orchestrates the entire workflow
- **LoggerService**: Provides logging to console and file

### UI
- **ConsoleUI**: Console-based user interface
- **Program**: Entry point with DI setup

## Extension Points

The application is designed to be extensible:

1. **New Quiz Platforms**: Add selectors in QuizDetector and QuestionExtractor
2. **New Video Players**: Add selectors in VideoSkipper
3. **New AI Providers**: Implement alternative to CopilotCliExecutor
4. **GUI**: Replace ConsoleUI with WPF/WinForms/Avalonia
5. **New Question Types**: Extend QuizQuestion model and extraction logic
6. **Custom Logging**: Implement additional ILogger providers

## Testing Strategy

- Unit tests for individual components
- Integration tests for service interactions
- End-to-end tests with mock course pages
- Browser automation tests with Playwright

## Security Considerations

- No credentials stored in code
- GitHub Copilot CLI uses system authentication
- Screenshots may contain sensitive information (stored locally)
- Generated specs reviewed before execution
- Browser runs in sandboxed environment

## Performance Optimization

- Headless mode for faster execution
- Parallel processing potential for multiple questions
- Caching of repeated operations
- Efficient selector strategies
- Network idle state detection

## Maintenance

- Modular architecture for easy updates
- Comprehensive logging for debugging
- Clear separation of concerns
- Extensible design patterns
- Version controlled dependencies
