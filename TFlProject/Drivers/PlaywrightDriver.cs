using Microsoft.Playwright;

namespace TFlProject.Drivers;

public sealed class PlaywrightDriver : IAsyncDisposable
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    public IPage Page { get; private set; } = default!;

    public async Task InitializeAsync(bool headless = false, int defaultTimeoutMs = 15000)
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new()
        {
            Headless = headless,
            SlowMo = headless ? 0 : 50
        });
        _context = await _browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            Locale = "en-GB"
        });
        Page = await _context.NewPageAsync();
        Page.SetDefaultTimeout(defaultTimeoutMs);
        Page.SetDefaultNavigationTimeout(45000);
    }

    public async ValueTask DisposeAsync()
    {
        try { if (_context != null) await _context.CloseAsync(); }
        finally
        {
            try { if (_browser != null) await _browser.CloseAsync(); }
            finally { _playwright?.Dispose(); }
        }
    }
}
