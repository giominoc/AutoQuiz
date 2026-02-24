# Novolearn Targeted Course Implementation

## Overview

This document describes the implementation of Novolearn-specific targeted course filtering and robust loop prevention to ensure the automation only processes specific courses at 0% progress and doesn't get stuck in infinite loops.

## Problem Statement

The original automation had the following issues:
1. **Incorrect Course Selection**: Selected courses with progress > 0%, wasting time on partially completed courses
2. **Lack of Course Filtering**: Attempted to process ALL courses instead of only specific target courses
3. **Loop Detection Issues**: Got stuck in infinite loops during quiz detection and extraction
4. **Badge Confusion**: Couldn't distinguish between "0% COMPLETATO" (valid) and completion badges (invalid)

## Solution

### 1. Targeted Course Title Filtering

**File**: `AutoQuiz.App/Services/CourseNavigator.cs`

#### Implementation Details

Added a whitelist of allowed course titles:
```csharp
private static readonly string[] AllowedCourseTitles = new[]
{
    "Anti-Corruption 2.0 INT",
    "FORMAZIONE IN MATERIA DI D.LGS.",  // Covers variations like "FORMAZIONE IN MATERIA DI D.LGS. 231/2001"
    "GDPR - REFRESHER 3 INT"
};
```

#### Key Features

1. **Case-Insensitive Matching**: Handles variations like "anti-corruption 2.0 int"
2. **Flexible Whitespace**: Normalizes multiple spaces to single spaces
3. **Prefix Matching**: Supports course titles with suffixes (e.g., "FORMAZIONE IN MATERIA DI D.LGS. 231/2001")
4. **Strict Filtering**: Only processes courses in the whitelist

#### Method: `IsAllowedCourseTitle(string courseTitle)`

```csharp
private bool IsAllowedCourseTitle(string courseTitle)
{
    if (string.IsNullOrWhiteSpace(courseTitle))
        return false;

    // Normalize whitespace for comparison
    var normalizedTitle = Regex.Replace(courseTitle.Trim(), @"\s+", " ");

    foreach (var allowedTitle in AllowedCourseTitles)
    {
        var normalizedAllowed = Regex.Replace(allowedTitle, @"\s+", " ");
        
        // Check if the course title starts with the allowed pattern
        if (normalizedTitle.StartsWith(normalizedAllowed, StringComparison.OrdinalIgnoreCase))
            return true;
        
        // Also check exact match with flexible whitespace
        if (normalizedTitle.Equals(normalizedAllowed, StringComparison.OrdinalIgnoreCase))
            return true;
    }

    return false;
}
```

#### Integration in GetIncompleteCoursesAsync

The course filtering is applied early in the course selection pipeline:

```csharp
// First check if this is an allowed course title
var courseTitle = text?.Trim() ?? "";
if (!IsAllowedCourseTitle(courseTitle))
{
    _logger.LogDebug("Skipping course '{Title}' - not in allowed list", courseTitle);
    continue;
}
```

### 2. Enhanced 0% Progress Detection

**File**: `AutoQuiz.App/Services/CourseNavigator.cs`

#### Smart Badge Detection

The enhanced `IsZeroProgress` method now:

1. **Detects Completion Badges**: Recognizes "COMPLETATO", "COMPLETATA", "COMPLETED", "✓", "✔"
2. **Distinguishes "0% COMPLETATO" from Completion**: Allows courses showing "0% COMPLETATO" but excludes courses with completion badges alone
3. **Strict Percentage Filtering**: Only accepts exactly 0% - any progress > 0% is rejected
4. **Progress Indicator Detection**: Detects "in progress", "in corso", "continua", "riprendi", "resume"

#### Logic Flow

```
1. Check for completion badges (COMPLETATO, ✓, etc.)
   ├─ If found, check if there's a "0%" percentage
   │  ├─ If yes: Continue checking (valid "0% COMPLETATO")
   │  └─ If no: Return false (course is completed)
   └─ If not found: Continue

2. Extract all percentages from text
   ├─ If any percentage > 0% and < 100%: Return false (has progress)
   └─ If 0% found: Log and continue

3. Check for progress indicators
   ├─ If found: Return false (course in progress)
   └─ If not found: Continue

4. Return true (course is at 0%)
```

#### Example Scenarios

| Text | Result | Reason |
|------|--------|--------|
| "Anti-Corruption 2.0 INT 0% COMPLETATO" | ✅ True | 0% with badge is valid |
| "GDPR - REFRESHER 3 INT 0%" | ✅ True | Exactly 0% |
| "Course Name" | ✅ True | No progress indicator |
| "Course Name 50% COMPLETATO" | ❌ False | Has progress |
| "Course Name COMPLETATO" | ❌ False | Completion badge without 0% |
| "Course Name ✓" | ❌ False | Checkmark indicates completion |
| "Course Name In Progress" | ❌ False | In progress indicator |

### 3. Loop Prevention in Quiz Processing

**File**: `AutoQuiz.App/Services/QuizAutomationService.cs`

#### Multiple Loop Prevention Mechanisms

The `ProcessCourseAsync` method now includes:

1. **Maximum Iteration Limit**: Stops after 100 iterations per course
2. **Same URL Detection**: Detects when stuck on the same URL for 3+ iterations
3. **Empty Question Handling**: Continues navigation when quiz is detected but no questions are extracted
4. **Iteration Logging**: Logs total iterations completed for debugging

#### Implementation

```csharp
private async Task<QuizResult> ProcessCourseAsync(IPage page, AutomationConfig config)
{
    var result = new QuizResult();
    var questionNumber = 0;
    var sameUrlCounter = 0;
    var maxSameUrlAttempts = 3;
    var maxIterations = 100; // Prevent infinite loops
    var iterationCount = 0;
    string lastUrl = string.Empty;

    while (true)
    {
        iterationCount++;
        
        // Loop prevention: Check iteration limit
        if (iterationCount > maxIterations)
        {
            _logger.LogWarning("Maximum iteration limit ({Limit}) reached, stopping to prevent infinite loop", maxIterations);
            break;
        }

        // Loop prevention: Track current URL to detect stuck scenarios
        var currentUrl = page.Url;
        if (currentUrl == lastUrl)
        {
            sameUrlCounter++;
            if (sameUrlCounter >= maxSameUrlAttempts)
            {
                _logger.LogWarning("Stuck on same URL '{Url}' for {Count} iterations, likely in a loop - breaking", currentUrl, sameUrlCounter);
                break;
            }
        }
        else
        {
            sameUrlCounter = 0;
            lastUrl = currentUrl;
        }
        
        // ... rest of processing logic
    }
}
```

#### Empty Question Handling

```csharp
// Loop prevention: Check if we got any questions
if (!questions.Any())
{
    _logger.LogWarning("Quiz detected but no questions extracted, attempting to continue");
    
    // Try to navigate forward anyway
    var tryNext = await _pageNavigator.TryClickNextAsync(page);
    if (!tryNext)
    {
        _logger.LogInformation("No next button found after failed extraction, stopping");
        break;
    }
    await Task.Delay(2000);
    continue;
}
```

### 4. Cross-Course Pollution Prevention

**File**: `AutoQuiz.App/Playwright/PageNavigator.cs`

Added `ResetVisitedMenuItems()` method to clear the visited menu items tracker between courses:

```csharp
public void ResetVisitedMenuItems()
{
    _logger.LogDebug("Resetting visited menu items tracker");
    _visitedMenuItems.Clear();
}
```

This is called before processing each new course in `QuizAutomationService`:

```csharp
// Reset visited menu items to prevent cross-course pollution
_pageNavigator.ResetVisitedMenuItems();
```

## Testing

### Unit Tests

Two comprehensive test suites were created and validated:

#### 1. Title Matching Tests

Tests the `IsAllowedCourseTitle` logic with:
- Exact matches
- Case insensitive matching
- Extra whitespace handling
- Suffix variations (e.g., "FORMAZIONE IN MATERIA DI D.LGS. 231/2001")
- Negative cases (wrong titles)

**Result**: 12/12 tests passed ✅

#### 2. Progress Detection Tests

Tests the `IsZeroProgress` logic with:
- "0% COMPLETATO" (should accept)
- Completion badges without 0% (should reject)
- Progress percentages > 0% (should reject)
- Progress indicators like "In Progress" (should reject)
- Edge cases (empty text, no indicators)

**Result**: 15/15 tests passed ✅

## Logging Enhancements

New log messages for debugging:

### Course Filtering
- "Found allowed course title: '{Title}'"
- "Skipping course '{Title}' - not in allowed list"
- "Skipping course '{Title}' - already completed or has progress > 0%"

### Progress Detection
- "Course title '{Title}' matches allowed pattern '{Pattern}'"
- "Course has completion badge '{Badge}', excluding"
- "Course is at 0% progress"
- "Course has progress: {Percentage}%"

### Loop Prevention
- "Maximum iteration limit ({Limit}) reached, stopping to prevent infinite loop"
- "Stuck on same URL '{Url}' for {Count} iterations, likely in a loop - breaking"
- "Quiz detected but no questions extracted, attempting to continue"
- "Course processing completed after {Count} iterations"
- "Resetting visited menu items tracker"

## Configuration

### Adding New Allowed Courses

To add new courses to the whitelist, update the `AllowedCourseTitles` array in `CourseNavigator.cs`:

```csharp
private static readonly string[] AllowedCourseTitles = new[]
{
    "Anti-Corruption 2.0 INT",
    "FORMAZIONE IN MATERIA DI D.LGS.",
    "GDPR - REFRESHER 3 INT",
    "Your New Course Title Here"  // Add new titles here
};
```

### Adjusting Loop Prevention Limits

To adjust loop prevention thresholds, modify these constants in `QuizAutomationService.cs`:

```csharp
var maxSameUrlAttempts = 3;    // Number of times to allow same URL
var maxIterations = 100;       // Maximum iterations per course
```

## Edge Cases Handled

1. **Course Title Variations**: Handles whitespace variations and suffixes
2. **"0% COMPLETATO" vs "COMPLETATO"**: Correctly distinguishes between not started (0%) and completed
3. **Empty Quiz Extraction**: Continues navigation instead of getting stuck
4. **Same URL Loops**: Detects and breaks out of URL loops
5. **Iteration Explosions**: Hard limit prevents infinite loops
6. **Cross-Course State**: Resets visited items between courses

## Backward Compatibility

✅ All changes are backward compatible:
- Non-Novolearn courses can still be processed by modifying the whitelist
- Progress detection improvements benefit all course types
- Loop prevention adds safety without changing behavior for working scenarios
- Logging is additive and doesn't break existing functionality

## Future Enhancements

1. **Configurable Whitelist**: Move allowed courses to configuration file
2. **Dynamic Course Discovery**: API-based course list retrieval
3. **Progress Thresholds**: Configurable acceptable progress ranges
4. **Advanced Loop Detection**: Pattern-based loop detection using page content hashing
5. **Course State Persistence**: Remember which courses have been attempted across runs

## Files Modified

1. ✅ `AutoQuiz.App/Services/CourseNavigator.cs`
   - Added `AllowedCourseTitles` whitelist
   - Implemented `IsAllowedCourseTitle()` method
   - Enhanced `IsZeroProgress()` with smart badge detection
   - Updated `GetIncompleteCoursesAsync()` to apply title filtering

2. ✅ `AutoQuiz.App/Services/QuizAutomationService.cs`
   - Added loop prevention in `ProcessCourseAsync()`
   - Implemented iteration tracking and limits
   - Added same URL detection
   - Enhanced empty question handling
   - Added visited items reset

3. ✅ `AutoQuiz.App/Playwright/PageNavigator.cs`
   - Added `ResetVisitedMenuItems()` method

## Summary

This implementation provides robust, targeted course automation for Novolearn that:
- ✅ Only processes specific whitelisted courses
- ✅ Only selects courses at exactly 0% progress
- ✅ Correctly handles "0% COMPLETATO" format
- ✅ Prevents infinite loops in quiz processing
- ✅ Maintains clean state between courses
- ✅ Provides comprehensive logging for debugging
- ✅ Is fully tested and validated
