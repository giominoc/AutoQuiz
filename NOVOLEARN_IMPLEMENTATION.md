# NOVOLEARN/SCORM Course Launch Implementation

## Overview

This document describes the implementation of NOVOLEARN/Novo Academy and SCORM course support in AutoQuiz. The implementation enables AutoQuiz to detect, launch, and navigate through courses that use SCORM-based content delivery systems, particularly those requiring explicit course launch buttons and popup windows.

## Problem Statement

AutoQuiz was detecting courses on NOVOLEARN/Novo Academy but failing to properly launch them. The issues included:

1. **Missing Launch Step**: Courses require clicking "Clicca per iniziare" (Click to start) button
2. **Popup Handling**: Lessons open in popup windows or new tabs (SCORM index_lms.html)
3. **Inner Start Button**: SCORM window contains an "INIZIO" button that must be clicked
4. **Navigation Issues**: Italian labels and SCORM UI controls were not supported
5. **Quiz Extraction**: Standard extraction methods didn't work with SCORM quiz formats

## Solution Architecture

### 1. CourseLauncher Service

**File**: `AutoQuiz.App/Services/CourseLauncher.cs`

A new service that handles the complete course launch workflow:

#### Key Methods:

- **TryLaunchCourseAsync(IPage page)**: Main entry point that orchestrates the launch process
  - Detects launch button ("Clicca per iniziare", "Inizia", "Avvia", etc.)
  - Handles popup window opening using `WaitForPopupAsync`
  - Falls back to iframe detection if no popup opens
  - Clicks the inner "INIZIO" start button
  - Returns the active page/frame context for subsequent operations

- **DetectLaunchButtonAsync(IPage page)**: Finds the course launch button
  - Supports Italian variants: "Clicca per iniziare", "Inizia", "Avvia", "Comincia"
  - Supports English variants: "Click to start", "Start", "Launch", "Begin"
  - Includes SCORM-specific selectors

- **DetectIframeContextAsync(IPage page)**: Detects SCORM iframes
  - Looks for frames with "index_lms.html" or "scorm" in URL
  - Returns page reference for iframe-based content

- **TryClickStartButtonAsync(IPage page)**: Clicks the inner start button
  - Italian: "INIZIO", "Inizia", "Avvia"
  - English: "START", "Begin"
  - Searches both in page and in iframes

### 2. Enhanced Navigation (PageNavigator)

**File**: `AutoQuiz.App/Playwright/PageNavigator.cs`

#### Improvements:

1. **Italian Language Support**:
   - "Avanti" (Next/Forward)
   - "Continua" (Continue)
   - "Successivo" (Next)
   - "Prosegui" (Proceed)

2. **SCORM Navigation Selectors**:
   - `[class*='next']`, `[id*='next']`
   - `[class*='forward']`, `[id*='forward']`
   - `[aria-label*='Next']`, `[aria-label*='Avanti']`

3. **Menu-Based Navigation Fallback** (`TryScormMenuNavigationAsync`):
   - Detects SCORM side menu items
   - Finds unlocked, incomplete items
   - Clicks next available lesson in menu
   - Handles Italian completion indicators: "completato", "completata"

### 3. Enhanced Quiz Detection

**File**: `AutoQuiz.App/QuizDetection/QuizDetector.cs`

#### Improvements:

1. **SCORM Quiz Selectors**:
   - `[class*='quiz']`, `[id*='quiz']`
   - `[class*='assessment']`, `[id*='assessment']`

2. **Italian Keywords**:
   - "domanda", "domande" (question/questions)
   - "risposta", "risposte" (answer/answers)
   - "seleziona", "scegli" (select, choose)
   - "quale", "cosa", "come", "perché" (which, what, how, why)
   - "domande finali" (final questions)

### 4. Enhanced Question Extraction

**File**: `AutoQuiz.App/QuizDetection/QuestionExtractor.cs`

#### Improvements:

1. **Italian Question Selectors**:
   - `h2:has-text('Domanda')`
   - `[class*='domanda']`
   - `.scorm-question`

2. **SCORM Answer Extraction**:
   - `[role='radio']`, `[role='checkbox']`
   - `[class*='choice']`, `[class*='option']`
   - `li[class*='answer']`, `div[class*='answer']`
   - `[class*='risposta']` (Italian for answer)

3. **Clickable List Items**:
   - Extracts answer options from `<ul>` and `<ol>` lists
   - Filters by reasonable text length
   - Detects single-line options

4. **Diagnostic Artifacts** (`CaptureDiagnosticsAsync`):
   - Captures full-page screenshot on extraction failure
   - Dumps HTML to file for analysis
   - Logs diagnostic information
   - Files saved to `Logs/Screenshots/diagnostic_*.png` and `Logs/diagnostic_*.html`

### 5. Enhanced Video Skipping

**File**: `AutoQuiz.App/Playwright/VideoSkipper.cs`

#### Improvements:

- Italian skip buttons: "Salta" (Skip), "Avanti" (Next)
- Visibility checks before clicking
- Additional class-based selectors

### 6. Integration with Main Service

**File**: `AutoQuiz.App/Services/QuizAutomationService.cs`

#### Changes:

1. **CourseLauncher Injection**: Added to constructor and DI container
2. **Launch Before Processing**: `ProcessCourseWithRetriesAsync` now calls `TryLaunchCourseAsync` before processing
3. **Page Context Tracking**: Uses returned page from CourseLauncher for all subsequent operations
4. **Italian Restart Buttons**: "Ricomincia", "Riprova", "Riavvia"

### 7. Dependency Injection Registration

**File**: `AutoQuiz.App/Program.cs`

Added `.AddSingleton<CourseLauncher>()` to service registration.

## Workflow Changes

### Before:
1. Navigate to course URL
2. ❌ Immediate "No next button found" error
3. ❌ Extraction fails: "Could not extract complete question data"
4. Result: 0% completion

### After:
1. Navigate to course URL
2. ✅ Detect and click "Clicca per iniziare" button
3. ✅ Wait for popup window to open
4. ✅ Switch to popup page context
5. ✅ Click "INIZIO" button to start lesson
6. ✅ Navigate through SCORM content using menu or buttons
7. ✅ Detect Italian quiz questions
8. ✅ Extract answers from SCORM format
9. ✅ Complete course with proper navigation

## Logging Enhancements

New log messages include:

- "Attempting to launch course..."
- "Launch button found, clicking..."
- "Popup opened with URL: {Url}"
- "Found SCORM iframe with URL: {Url}"
- "Found start button: {Text}"
- "Successfully launched course, now in lesson context"
- "Attempting SCORM menu navigation..."
- "Found {Count} menu items with selector: {Selector}"
- "Clicking next menu item: {Text}"
- "Capturing diagnostic artifacts for failed extraction..."

## Edge Cases Handled

1. **No Launch Button**: Returns original page if no launch button detected
2. **Popup Timeout**: Falls back to iframe detection if popup doesn't open within 5 seconds
3. **No Iframe**: Uses original page if neither popup nor iframe detected
4. **No Start Button**: Continues anyway as course may auto-start
5. **NetworkIdle Timeout**: Continues navigation for SCORM pages that don't trigger network events
6. **Extraction Failures**: Captures diagnostics for troubleshooting

## Testing Recommendations

To test the implementation:

1. **NOVOLEARN Course with Launch Button**:
   - Verify launch button is detected and clicked
   - Verify popup opens and is tracked
   - Verify INIZIO button is clicked

2. **SCORM Menu Navigation**:
   - Verify menu items are detected
   - Verify unlocked items are clicked in sequence

3. **Italian Quiz Detection**:
   - Verify Italian questions are detected
   - Verify Italian answers are extracted

4. **Diagnostic Capture**:
   - Intentionally fail extraction
   - Verify screenshot and HTML are saved

## Future Enhancements

1. **Multiple Popup Handling**: Track multiple SCORM windows simultaneously
2. **Advanced Frame Navigation**: More sophisticated iframe switching
3. **SCORM Progress Tracking**: Parse SCORM completion status
4. **More Italian Variants**: Additional regional language support
5. **SCORM Standard Compliance**: Implement full SCORM API interaction

## Files Modified

1. ✅ `AutoQuiz.App/Services/CourseLauncher.cs` (NEW)
2. ✅ `AutoQuiz.App/Playwright/PageNavigator.cs`
3. ✅ `AutoQuiz.App/QuizDetection/QuizDetector.cs`
4. ✅ `AutoQuiz.App/QuizDetection/QuestionExtractor.cs`
5. ✅ `AutoQuiz.App/Playwright/VideoSkipper.cs`
6. ✅ `AutoQuiz.App/Services/QuizAutomationService.cs`
7. ✅ `AutoQuiz.App/Program.cs`

## Compatibility

- ✅ Backward compatible with existing course platforms
- ✅ Gracefully falls back when SCORM features not detected
- ✅ English language courses continue to work
- ✅ Standard quiz formats still supported

## Dependencies

No new external dependencies added. Implementation uses existing:
- Microsoft.Playwright for popup and iframe handling
- Microsoft.Extensions.Logging for logging
- Existing AutoQuiz architecture and patterns
