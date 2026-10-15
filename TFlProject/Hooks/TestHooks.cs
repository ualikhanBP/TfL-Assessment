using Microsoft.Playwright;
using NUnit.Framework;
using Reqnroll;
using TFlProject.Drivers;

namespace TFlProject.Hooks;

[Binding]
public sealed class TestHooks
{
    private readonly ScenarioContext _context;
    private PlaywrightDriver? _driver;
    public TestHooks(ScenarioContext context) => _context = context;

    [BeforeScenario]
    public async Task BeforeScenario()
    {
        _driver = new PlaywrightDriver();
        await _driver.InitializeAsync(headless: false);
        _context.Set(_driver);
        _driver.Page.PageError += (_, e) => TestContext.Progress.WriteLine($"JS ERROR: {e}");
        _driver.Page.RequestFailed += (_, r) => TestContext.Progress.WriteLine($"REQUEST FAILED: {r.Url} {r.Failure}");
    }

    [AfterScenario]
    public async Task AfterScenario()
    {
        if (_driver == null) return;
        try
        {
            if (_context.TestError != null && !_driver.Page.IsClosed)
            {
                try
                {
                    var dir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "TestResults", "Screenshots");
                    Directory.CreateDirectory(dir);
                    var title = string.Concat(_context.ScenarioInfo.Title.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
                    var path = Path.Combine(dir, $"{title}-{Guid.NewGuid():N}.png");
                    await _driver.Page.ScreenshotAsync(new() { Path = path, FullPage = true, Timeout = 5000 });
                    TestContext.AddTestAttachment(path, "Failure screenshot");
                }
                catch (Exception e) { TestContext.Progress.WriteLine($"Screenshot unavailable: {e.Message}"); }
            }
        }
        finally
        {
            try { await _driver.DisposeAsync(); }
            catch (Exception e) { TestContext.Progress.WriteLine($"Browser cleanup: {e.Message}"); }
        }
    }
}
