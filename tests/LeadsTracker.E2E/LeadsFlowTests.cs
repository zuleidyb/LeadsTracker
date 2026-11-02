using System.Net.Http.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Xunit;

namespace LeadsTracker.E2E;

public sealed class LeadsFlowTests : IClassFixture<StackFixture>, IDisposable
{
    private readonly StackFixture _stack;
    private readonly IWebDriver _driver;
    private readonly LeadsApp _app;
    private readonly string _token = $"e2e{Guid.NewGuid():N}";

    public LeadsFlowTests(StackFixture stack)
    {
        _stack = stack;

        var options = new ChromeOptions();
        options.AddArgument("--window-size=1400,1200");

        if (Environment.GetEnvironmentVariable("E2E_HEADED") != "1")
        {
            options.AddArguments("--headless=new", "--no-sandbox", "--disable-dev-shm-usage");
        }

        _driver = new ChromeDriver(options);
        _app = new LeadsApp(_driver, stack.BaseUrl);
        _app.Open();
    }

    public void Dispose() => _driver.Quit();

    [Fact]
    public void New_lead_appears_on_the_dashboard()
    {
        var email = $"{_token}@example.test";

        _app.OpenNewLeadForm();
        _app.FillRequiredFields(new LeadData(email, _token, "Maya", "Chen"));
        _app.Save();

        _app.WaitForText("notice", "Maya Chen was added.");
        _app.Search(_token);
        _app.WaitForRowCount(1);
        _app.WaitForText("lead-email", email);
    }

    [Fact]
    public void Empty_form_shows_required_field_errors()
    {
        _app.OpenNewLeadForm();
        _app.Save();

        _app.WaitForText("error-firstName", "Enter a first name.");
        _app.WaitForText("error-lastName", "Enter a last name.");
        _app.WaitForText("error-email", "Enter an email.");
        _app.WaitForText("error-company", "Enter a company.");
        _app.WaitForText("error-source", "Choose a lead source.");
    }

    [Fact]
    public async Task Duplicate_email_shows_the_api_error_on_the_form()
    {
        var email = $"{_token}@example.test";
        await Seed(email);

        _app.OpenNewLeadForm();
        _app.FillRequiredFields(new LeadData(email.ToUpperInvariant(), _token));
        _app.Save();

        _app.WaitForText("error-email", "A lead with this email already exists.");
        _app.WaitForVisible("lead-form");
    }

    [Fact]
    public async Task Filters_update_the_list_and_totals_and_can_be_cleared()
    {
        await Seed($"{_token}a@example.test", "Qualified");
        await Seed($"{_token}b@example.test", "New");

        _app.Search(_token);
        _app.WaitForRowCount(2);
        _app.WaitForText("total-Qualified", "1");
        _app.WaitForText("total-New", "1");

        _app.FilterByStatus("Qualified");
        _app.WaitForRowCount(1);
        _app.WaitForText("total-Qualified", "1");
        _app.WaitForText("total-New", "0");

        _app.ClearFilters();
        Assert.Equal("", _app.ValueOf("filter-search"));
        Assert.Equal("", _app.ValueOf("filter-status"));
    }

    [Fact]
    public async Task Pagination_moves_to_the_next_page()
    {
        for (var index = 0; index < 11; index++)
        {
            await Seed($"{_token}{index}@example.test");
        }

        _app.Search(_token);
        _app.WaitForText("pager-range", "Showing 1-10 of 11");
        _app.WaitForRowCount(10);

        _app.NextPage();
        _app.WaitForText("pager-range", "Showing 11-11 of 11");
        _app.WaitForRowCount(1);
    }

    [Fact]
    public void Search_with_no_matches_shows_the_empty_message()
    {
        _app.Search(_token);

        _app.WaitForText("no-results", "No leads match these filters.");
        _app.WaitForRowCount(0);
    }

    private async Task Seed(string email, string status = "New")
    {
        var response = await _stack.Http.PostAsJsonAsync("/api/leads", new
        {
            firstName = "Seed",
            lastName = "Lead",
            email,
            company = _token,
            status,
            source = "Website"
        });

        response.EnsureSuccessStatusCode();
    }
}
