using Microsoft.Playwright;

namespace TFlProject.Pages;

public sealed class JourneyDetailsPage
{
    private readonly IPage _page;
    public JourneyDetailsPage(IPage page) => _page = page;

    public async Task OpenDetailsAsync()
    {
        var button = _page.Locator("button.view-hide-details:visible").First;
        await button.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30000 });
        await button.ClickAsync();
    }

    public async Task VerifyDestinationAccessAsync(string destination)
    {
        var station = _page.GetByText(destination, new() { Exact = false }).First;
        await station.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
        var info = _page.Locator("div.access-information:visible").First;
        await info.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
        var labels = info.Locator("a[aria-label]");
        if (await labels.CountAsync() == 0)
            throw new InvalidOperationException("Access-information icons with labels were not displayed.");
        
    }
}
