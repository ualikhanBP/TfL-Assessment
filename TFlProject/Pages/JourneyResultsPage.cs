using System.Text.RegularExpressions;
using NUnit.Framework;
using Microsoft.Playwright;

namespace TFlProject.Pages;

public sealed class JourneyResultsPage
{
    private readonly IPage _page;
    public JourneyResultsPage(IPage page) => _page = page;
    private ILocator Cycling => _page.Locator("a.journey-box.cycling:visible").First;
    private ILocator Walking => _page.Locator("a.journey-box.walking:visible").First;

    public async Task WaitForJourneyAsync()
    {
        
        await _page.GetByRole(AriaRole.Heading, new() { Name = "Journey results" })
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30000 });

        var result = _page.Locator(
            "a.journey-box.cycling:visible, a.journey-box.walking:visible, " +
            "button.view-hide-details:visible").First;
        var noResults = _page.GetByText(
            "Journey planner could not find any results", new() { Exact = false }).First;

        try
        {
            await result.Or(noResults).First.WaitForAsync(new()
            {
                State = WaitForSelectorState.Visible,
                Timeout = 30000
            });
        }
        catch (TimeoutException)
        {
            TestContext.Progress.WriteLine($"No recognised result cards at {_page.Url}");
            throw;
        }
        if (await noResults.IsVisibleAsync())
            throw new InvalidOperationException(
                "TfL returned no journeys for this date/time; cannot validate preferences.");
    }

    public async Task<int> AlternativeMinutesAsync(string mode)
    {
        var card = mode == "cycling" ? Cycling : Walking;
        await card.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30000 });
        var info = card.Locator("div.col2.journey-info").First;
        var text = await info.InnerTextAsync();
        var match = Regex.Match(text, @"(?:(\d+)\s*(?:hours?|hrs?|h)\s*)?(\d+)\s*(?:minutes?|mins?|m)\b", RegexOptions.IgnoreCase);
        if (match.Success)
            return (match.Groups[1].Success ? int.Parse(match.Groups[1].Value) * 60 : 0) + int.Parse(match.Groups[2].Value);
        var number = Regex.Match(text.Trim(), @"^\d+$");
        if (number.Success) return int.Parse(number.Value);
        throw new FormatException($"Cannot parse {mode} duration from '{text}'.");
    }

    public async Task<string> JourneyDurationAsync()
    {
        await WaitForJourneyAsync();
        var duration = _page.Locator("div.time-boxes.time-boxes-override:visible " +
            "div.journey-time.no-map:visible").First;
        if (await duration.CountAsync() > 0)
        {
            var value = (await duration.InnerTextAsync()).Trim();
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }

       
        var walking = _page.Locator("a.journey-box.walking:visible").First;
        if (await walking.CountAsync() > 0)
            return $"{await AlternativeMinutesAsync("walking")} mins (walking)";
        var cycling = _page.Locator("a.journey-box.cycling:visible").First;
        if (await cycling.CountAsync() > 0)
            return $"{await AlternativeMinutesAsync("cycling")} mins (cycling)";
        throw new InvalidOperationException("No journey or alternative travel duration visible.");
    }

    public async Task OpenPreferencesAsync()
    {
        await WaitForJourneyAsync();
        var edit = _page.Locator("button.toggle-options.more-options:has-text('Edit preferences')").First;
        await edit.ClickAsync();
        await _page.Locator("#JourneyPreference_2").WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 15000 });
    }
    public async Task SelectLeastWalkingAsync()
    {
        var radio = _page.Locator("#JourneyPreference_2");
        await radio.CheckAsync(new() { Force = true }); 
        if (!await radio.IsCheckedAsync()) throw new InvalidOperationException("Least walking not selected.");
    }
    public async Task UpdateAsync()
    {
        
        var update = _page.Locator(
            "input[type='submit'][value='Update journey']:visible, " +
            "button:visible:has-text('Update journey')").First;

        if (await update.CountAsync() == 0)
        {
            var candidates = _page.Locator("#plan-journey-button");
            var count = await candidates.CountAsync();
            TestContext.Progress.WriteLine($"Update journey: {count} matching ID elements found.");
            for (var i = 0; i < count; i++)
            {
                var item = candidates.Nth(i);
                TestContext.Progress.WriteLine(
                    $"Candidate {i}: visible={await item.IsVisibleAsync()}, " +
                    $"enabled={await item.IsEnabledAsync()}");
            }
            throw new InvalidOperationException(
                "No visible 'Update journey' submit control found after selecting least walking. " +
                "Inspect the expanded Edit preferences panel and the failure screenshot.");
        }

        await update.ScrollIntoViewIfNeededAsync();
        await update.ClickAsync(new() { Timeout = 15000 });
        await WaitForJourneyAsync();
    }
    public async Task<bool> IsLeastWalkingSelectedAsync()
    {
        var radio = _page.Locator("#JourneyPreference_2");
        return await radio.CountAsync() > 0 && await radio.IsCheckedAsync();
    }
}
