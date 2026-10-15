using Microsoft.Playwright;
using NUnit.Framework;

namespace TFlProject.Pages;

public sealed class JourneyPlannerPage
{
    private readonly IPage _page;
    public JourneyPlannerPage(IPage page) => _page = page;
    private ILocator From => _page.Locator("#InputFrom");
    private ILocator To => _page.Locator("#InputTo");
    private ILocator Submit => _page.Locator("#plan-journey-button");
    private ILocator Overlay => _page.Locator("#cb-cookieoverlay");
    private const string Url = "https://tfl.gov.uk/plan-a-journey";

    public async Task NavigateAsync()
    {
        Exception? last = null;
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            if (_page.IsClosed)
                throw new InvalidOperationException("Browser page closed before navigation; check browser crash or test teardown.", last);

            try
            {
                TestContext.Progress.WriteLine($"Navigation attempt {attempt}/3");
                var response = attempt == 1
                    ? await _page.GotoAsync(Url, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 45000 })
                    : await _page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 45000 });

                TestContext.Progress.WriteLine($"HTTP {response?.Status}, URL {_page.Url}");
                if (response is { Status: >= 400 })
                    throw new InvalidOperationException($"TfL returned HTTP {response.Status} at {_page.Url}");

                await From.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
                await HandleCookiesAsync(initial: true);
                await EnsureUnblockedAsync();
                TestContext.Progress.WriteLine("TfL journey form ready.");
                return;
            }
            catch (Exception ex) when (ex is TimeoutException || ex is PlaywrightException)
            {
                last = ex;
                TestContext.Progress.WriteLine($"Navigation attempt {attempt} failed: {ex.GetType().Name}: {ex.Message}");
                if (_page.IsClosed)
                    throw new InvalidOperationException("Browser page closed during navigation.", ex);
                if (attempt == 3) break;
            }
        }
        throw new InvalidOperationException("TfL journey form was not ready after three attempts; inspect HTTP and request logs.", last);
    }

    private async Task HandleCookiesAsync(bool initial = false)
    {
       
        if (initial)
        {
            try
            {
                await Overlay.WaitForAsync(new()
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 6000
                });
            }
            catch (TimeoutException)
            {
                
            }
        }

        if (!await Overlay.IsVisibleAsync()) return;

        var button = _page.GetByRole(AriaRole.Button, new()
        {
            Name = "Accept only essential cookies",
            Exact = true
        }).First;

        var clicked = await TryClickAsync(button);
        if (!clicked)
        {
            foreach (var frame in _page.Frames.Where(f => f != _page.MainFrame))
            {
                if (await TryClickAsync(frame.GetByRole(AriaRole.Button, new()
                {
                    Name = "Accept only essential cookies",
                    Exact = true
                }).First))
                {
                    clicked = true;
                    break;
                }
            }
        }

        if (!clicked)
        {
            
            clicked = await TryClickAsync(_page.Locator(
                "#CybotCookiebotDialogBodyLevelButtonLevelOptinDeclineAll, " +
                "#onetrust-reject-all-handler").First);
        }

        if (!clicked)
            throw new InvalidOperationException(
                "TfL cookie overlay is visible, but the essential-cookies button was not clickable.");

        TestContext.Progress.WriteLine("Essential cookie consent clicked. Waiting for overlay to clear.");
        await EnsureCookieOverlayClearedAsync();
    }


    private async Task EnsureCookieOverlayClearedAsync()
    {
        if (!await Overlay.IsVisibleAsync()) return;

        try
        {
            await Overlay.WaitForAsync(new()
            {
                State = WaitForSelectorState.Hidden,
                Timeout = 10000
            });
            return;
        }
        catch (TimeoutException)
        {
            TestContext.Progress.WriteLine(
                "Cookie overlay remained after consent. Reloading the page once.");
        }

        if (_page.IsClosed)
            throw new InvalidOperationException("Browser page closed before cookie recovery reload.");

        await _page.ReloadAsync(new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 45000
        });

        await From.WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 30000
        });

        try
        {
            await Overlay.WaitForAsync(new()
            {
                State = WaitForSelectorState.Hidden,
                Timeout = 15000
            });
        }
        catch (TimeoutException ex)
        {
            throw new InvalidOperationException(
                "Cookie overlay is still visible after accepting cookies and one page reload.", ex);
        }

        TestContext.Progress.WriteLine("Cookie overlay cleared after refresh.");
    }

    private static async Task<bool> TryClickAsync(ILocator button)
    {
        try
        {
            await button.WaitForAsync(new()
            {
                State = WaitForSelectorState.Visible,
                Timeout = 2500
            });
            await button.ClickAsync(new() { Timeout = 5000 });
            return true;
        }
        catch (TimeoutException) { return false; }
        catch (PlaywrightException) { return false; }
    }

    private async Task EnsureUnblockedAsync()
    {
        await HandleCookiesAsync();
        await EnsureCookieOverlayClearedAsync();
    }

    public async Task SelectFromAsync(string fullStation) => await SelectSuggestionAsync(From, fullStation);
    public async Task SelectToAsync(string fullStation) => await SelectSuggestionAsync(To, fullStation);

    private async Task SelectSuggestionAsync(ILocator input, string fullStation)
    {
        await EnsureUnblockedAsync();
        var partial = fullStation.Contains("Leicester Square", StringComparison.OrdinalIgnoreCase) ? "Leicester" :
            fullStation.Contains("Covent Garden", StringComparison.OrdinalIgnoreCase) ? "Covent" :
            fullStation.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        await input.FillAsync(partial);
        var option = _page.GetByRole(AriaRole.Option, new() { Name = fullStation, Exact = true }).First;
        try
        {
            await option.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 8000 });
            await option.ClickAsync();
        }
        catch (TimeoutException)
        {
            var fallback = _page.Locator("li").Filter(new() { HasTextString = fullStation }).First;
            await fallback.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 8000 });
            await fallback.ClickAsync();
        }
    }

    public async Task EnterRawFromAsync(string text)
    {
        await EnsureUnblockedAsync();
        await From.FillAsync(text);
        await From.PressAsync("Tab");
    }
    public async Task EnterRawToAsync(string text)
    {
        await EnsureUnblockedAsync();
        await To.FillAsync(text);
        await To.PressAsync("Tab");
    }
    public async Task SubmitAsync()
    {
        await EnsureUnblockedAsync();
        try { await Submit.ClickAsync(new() { Timeout = 15000 }); }
        catch (TimeoutException)
        {
            if (!await Overlay.IsVisibleAsync()) throw;
            await HandleCookiesAsync();
            await Submit.ClickAsync(new() { Timeout = 15000 });
        }
    }
    public async Task<string> ReadValidationAsync()
    {
        var error = _page.Locator(
            ".field-validation-error:visible, .validation-summary-errors:visible, " +
            ".journey-planner__error:visible, #InputFrom-error:visible, #InputTo-error:visible, " +
            "span:visible:has-text('We found more than one location matching')").First;
        await error.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
        return (await error.InnerTextAsync()).Trim();
    }
}
