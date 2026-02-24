using Microsoft.Playwright;
using Microsoft.Extensions.Logging;

namespace AutoQuiz.App.Playwright;

public class BrowserManager : IDisposable
{
    private readonly ILogger<BrowserManager> _logger;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IPage? _page;
    private bool _disposed;

    public BrowserManager(ILogger<BrowserManager> logger)
    {
        _logger = logger;
    }

    public async Task<IPage> InitializeAsync(bool headless = true)
    {
        _logger.LogInformation("Initializing browser in {Mode} mode", headless ? "headless" : "headed");
        
        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = headless,
            Args = new[] { "--start-maximized" }
        });

        _page = await _browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = ViewportSize.NoViewport
        });

        _logger.LogInformation("Browser initialized successfully");
        return _page;
    }

    public IPage? GetPage() => _page;

    public void Dispose()
    {
        if (_disposed) return;

        _browser?.CloseAsync().GetAwaiter().GetResult();
        _playwright?.Dispose();
        _disposed = true;

        _logger.LogInformation("Browser disposed");
    }
}
