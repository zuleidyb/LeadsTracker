using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace LeadsTracker.E2E;

public sealed record LeadData(string Email, string Company, string FirstName = "Test", string LastName = "Lead");

public sealed class LeadsApp
{
    private readonly IWebDriver _driver;
    private readonly string _baseUrl;
    private readonly WebDriverWait _wait;

    public LeadsApp(IWebDriver driver, string baseUrl)
    {
        _driver = driver;
        _baseUrl = baseUrl;
        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        _wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));
    }

    public void Open() => _driver.Navigate().GoToUrl(_baseUrl);

    public void OpenNewLeadForm() => Visible("nav-new-lead").Click();

    public void Save() => Visible("save-lead").Click();

    public void Search(string term) => Fill("filter-search", term);

    public void FilterByStatus(string status) => Choose("filter-status", status);

    public void ClearFilters() => Visible("clear-filters").Click();

    public void NextPage() => Visible("page-next").Click();

    public void FillRequiredFields(LeadData lead)
    {
        Fill("lead-firstName", lead.FirstName);
        Fill("lead-lastName", lead.LastName);
        Fill("lead-email-input", lead.Email);
        Fill("lead-company", lead.Company);
        Choose("lead-source", "Website");
    }

    public void WaitForText(string testId, string expected) =>
        _wait.Until(driver => driver.FindElement(TestId(testId)).Text == expected);

    public void WaitForRowCount(int expected) =>
        _wait.Until(driver => driver.FindElements(TestId("lead-row")).Count == expected);

    public void WaitForVisible(string testId) => Visible(testId);

    public string ValueOf(string testId) => Visible(testId).GetAttribute("value") ?? "";

    private void Fill(string testId, string value)
    {
        var element = Visible(testId);
        element.Clear();
        element.SendKeys(value);
    }

    private void Choose(string testId, string value) =>
        new SelectElement(Visible(testId)).SelectByValue(value);

    private IWebElement Visible(string testId) =>
        _wait.Until(driver =>
        {
            var element = driver.FindElement(TestId(testId));
            return element.Displayed ? element : null!;
        });

    private static By TestId(string testId) => By.CssSelector($"[data-testid='{testId}']");
}
