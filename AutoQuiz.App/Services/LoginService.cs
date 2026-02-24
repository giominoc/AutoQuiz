using Microsoft.Playwright;
using Microsoft.Extensions.Logging;

namespace AutoQuiz.App.Services;

public class LoginService
{
    private readonly ILogger<LoginService> _logger;

    // Login page URL patterns for detection
    private static readonly string[] LoginPagePatterns = new[]
    {
        "/login",
        "/signin",
        "/sign-in"
    };

    public LoginService(ILogger<LoginService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> LoginAsync(IPage page, string username, string password)
    {
        _logger.LogInformation("Attempting to login with username: {Username}", username);

        try
        {
            // Wait for the page to be ready
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Common username field selectors
            var usernameSelectors = new[]
            {
                "input[type='email']",
                "input[type='text'][name='username']",
                "input[type='text'][name='email']",
                "input[id='username']",
                "input[id='email']",
                "input[name='username']",
                "input[name='email']",
                "input[placeholder*='username' i]",
                "input[placeholder*='email' i]",
                "#username",
                "#email"
            };

            // Common password field selectors
            var passwordSelectors = new[]
            {
                "input[type='password']",
                "input[name='password']",
                "input[id='password']",
                "#password"
            };

            // Common submit button selectors
            var submitSelectors = new[]
            {
                "button[type='submit']",
                "input[type='submit']",
                "button:has-text('Log in')",
                "button:has-text('Sign in')",
                "button:has-text('Login')",
                "button:has-text('Submit')",
                "[data-purpose='submit-button']",
                ".submit-button"
            };

            // Find and fill username field
            IElementHandle? usernameField = null;
            foreach (var selector in usernameSelectors)
            {
                usernameField = await page.QuerySelectorAsync(selector);
                if (usernameField != null)
                {
                    _logger.LogInformation("Found username field with selector: {Selector}", selector);
                    await usernameField.FillAsync(username);
                    break;
                }
            }

            if (usernameField == null)
            {
                _logger.LogError("Could not find username field");
                return false;
            }

            // Find and fill password field
            IElementHandle? passwordField = null;
            foreach (var selector in passwordSelectors)
            {
                passwordField = await page.QuerySelectorAsync(selector);
                if (passwordField != null)
                {
                    _logger.LogInformation("Found password field with selector: {Selector}", selector);
                    await passwordField.FillAsync(password);
                    break;
                }
            }

            if (passwordField == null)
            {
                _logger.LogError("Could not find password field");
                return false;
            }

            // Find and click submit button
            IElementHandle? submitButton = null;
            foreach (var selector in submitSelectors)
            {
                submitButton = await page.QuerySelectorAsync(selector);
                if (submitButton != null)
                {
                    _logger.LogInformation("Found submit button with selector: {Selector}", selector);
                    await submitButton.ClickAsync();
                    break;
                }
            }

            if (submitButton == null)
            {
                _logger.LogError("Could not find submit button");
                return false;
            }

            // Wait for navigation after login
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions
            {
                Timeout = 30000
            });

            // Verify login success by checking for common error indicators
            var errorSelectors = new[]
            {
                ":has-text('Invalid')",
                ":has-text('incorrect')",
                ":has-text('wrong')",
                ":has-text('failed')",
                ".error-message",
                ".alert-error",
                "[role='alert']"
            };

            foreach (var selector in errorSelectors)
            {
                var errorElement = await page.QuerySelectorAsync(selector);
                if (errorElement != null)
                {
                    var errorText = await errorElement.TextContentAsync();
                    _logger.LogError("Login failed with error: {Error}", errorText);
                    return false;
                }
            }

            // Check if we're still on the login page (another indicator of failure)
            var currentUrl = page.Url;
            foreach (var pattern in LoginPagePatterns)
            {
                if (currentUrl.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError("Still on login page after submission");
                    return false;
                }
            }

            _logger.LogInformation("Login completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return false;
        }
    }
}
