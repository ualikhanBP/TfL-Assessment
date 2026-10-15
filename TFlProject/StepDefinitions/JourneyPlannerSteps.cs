using System.Text.RegularExpressions;
using NUnit.Framework;
using Microsoft.Playwright;
using Reqnroll;
using TFlProject.Drivers;
using TFlProject.Pages;

namespace TFlProject.StepDefinitions;

[Binding]
public sealed class JourneyPlannerSteps
{
    private readonly ScenarioContext _context;
    public JourneyPlannerSteps(ScenarioContext context) => _context = context;
    private IPage Page => _context.Get<PlaywrightDriver>().Page;
    private JourneyPlannerPage Planner => new(Page);
    private JourneyResultsPage Results => new(Page);
    private JourneyDetailsPage Details => new(Page);
    private string? _before;

    [Given(@"I open the TfL Journey Planner")]
    public Task Open() => Planner.NavigateAsync();

    [When(@"I select From ""([^""]+)"" from suggestions")]
    public Task From(string station) => Planner.SelectFromAsync(station);

    [When(@"I select To ""([^""]+)"" from suggestions")]
    public Task To(string station) => Planner.SelectToAsync(station);

    [When(@"I submit the journey")]
    public Task Submit() => Planner.SubmitAsync();

    [Then(@"the cycling time should be ""([^""]+)""")]
    public async Task Cycling(string expected) =>
        Assert.That(await Results.AlternativeMinutesAsync("cycling"), Is.EqualTo(ParseMinutes(expected)));

    [Then(@"the walking time should be ""([^""]+)""")]
    public async Task Walking(string expected) =>
        Assert.That(await Results.AlternativeMinutesAsync("walking"), Is.EqualTo(ParseMinutes(expected)));

    [Given(@"a journey is planned from ""([^""]+)"" to ""([^""]+)""")]
    public async Task Planned(string from, string to)
    {
        await Planner.SelectFromAsync(from);
        await Planner.SelectToAsync(to);
        await Planner.SubmitAsync();
        await Results.WaitForJourneyAsync();
        _before = await Results.JourneyDurationAsync();
    }

    [When(@"I open Edit preferences")]
    public Task Preferences() => Results.OpenPreferencesAsync();

    [When(@"I choose routes with least walking")]
    public Task LeastWalking() => Results.SelectLeastWalkingAsync();

    [When(@"I update the journey")]
    public Task Update() => Results.UpdateAsync();

    [Then(@"the updated journey time should be displayed")]
    public async Task UpdatedTime()
    {
        var after = await Results.JourneyDurationAsync();
        Assert.That(after, Is.Not.Empty);
        Assert.That(await Results.IsLeastWalkingSelectedAsync(), Is.True,
            "Least-walking selection was not retained.");
        TestContext.Progress.WriteLine($"Duration before: {_before}; after: {after}");
    }

    [When(@"I open View details")]
    public Task ViewDetails() => Details.OpenDetailsAsync();

    [Then(@"access information should appear for ""([^""]+)""")]
    public Task Access(string destination) => Details.VerifyDestinationAccessAsync(destination);

    [When(@"I enter invalid From ""([^""]+)""")]
    public Task InvalidFrom(string value) => Planner.EnterRawFromAsync(value);

    [When(@"I enter invalid To ""([^""]+)""")]
    public Task InvalidTo(string value) => Planner.EnterRawToAsync(value);

    [Then(@"a location validation error should be displayed")]
    public async Task InvalidError()
    {
        var error = await Planner.ReadValidationAsync();
        Assert.That(error, Is.Not.Empty);
        Assert.That(await Page.Locator("button.view-hide-details:visible").CountAsync(), Is.Zero,
            "Unexpected journey result for invalid input.");
    }

    [Then(@"required location validation should be displayed")]
    public async Task EmptyError()
    {
        var error = await Planner.ReadValidationAsync();
        Assert.That(error, Is.Not.Empty);
        Assert.That(await Page.Locator("button.view-hide-details:visible").CountAsync(), Is.Zero);
    }

    private static int ParseMinutes(string text)
    {
        var match = Regex.Match(text, @"^(\d+)\s+minutes?$", RegexOptions.IgnoreCase);
        if (!match.Success) throw new ArgumentException($"Invalid expected duration: {text}");
        return int.Parse(match.Groups[1].Value);
    }
}
