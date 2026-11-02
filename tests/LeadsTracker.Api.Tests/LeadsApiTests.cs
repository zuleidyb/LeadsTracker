using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LeadsTracker.Api.Contracts;
using LeadsTracker.Api.Data;
using LeadsTracker.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LeadsTracker.Api.Tests;

public class LeadsApiTests(ApiFactory factory) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LeadsDbContext>();
        await db.Leads.ExecuteDeleteAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Create_returns_201_and_stores_email_in_lowercase()
    {
        var response = await Post(NewLead("Maya.Chen@NorthPeak.example"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var lead = await response.Content.ReadFromJsonAsync<LeadResponse>(Json);
        Assert.Equal("maya.chen@northpeak.example", lead!.Email);
        Assert.NotEqual(Guid.Empty, lead.Id);
    }

    [Fact]
    public async Task Create_returns_409_when_email_differs_only_by_case()
    {
        await Post(NewLead("maya.chen@northpeak.example"));

        var response = await Post(NewLead("MAYA.CHEN@northpeak.example"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(
            "A lead with this email already exists.",
            problem.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Create_returns_400_when_email_is_missing()
    {
        var response = await Post(new { firstName = "No", lastName = "Email", company = "Test Co", status = "New", source = "Website" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_combines_filters_and_totals_follow_them()
    {
        await Post(NewLead("a@one.example", status: "Qualified", industry: "Technology"));
        await Post(NewLead("b@two.example", status: "Qualified", industry: "Retail"));
        await Post(NewLead("c@three.example", status: "New", industry: "Technology"));

        var result = await List("status=Qualified&industry=Technology");

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("a@one.example", Assert.Single(result.Items).Email);
        Assert.Equal(1, result.StatusCounts[LeadStatus.Qualified]);
        Assert.Equal(0, result.StatusCounts[LeadStatus.New]);
    }

    [Theory]
    [InlineData("maya chen")]
    [InlineData("NORTHPEAK")]
    [InlineData("northpeak.example")]
    public async Task List_search_matches_name_company_and_email(string term)
    {
        await Post(NewLead("maya.chen@northpeak.example", firstName: "Maya", lastName: "Chen", company: "Northpeak Systems"));
        await Post(NewLead("l.bouchard@riverbend.example", firstName: "Lucas", lastName: "Bouchard", company: "Riverbend Freight"));

        var result = await List($"q={Uri.EscapeDataString(term)}");

        Assert.Equal("maya.chen@northpeak.example", Assert.Single(result.Items).Email);
    }

    [Fact]
    public async Task List_search_treats_wildcards_as_plain_text()
    {
        await Post(NewLead("a@one.example"));

        var result = await List("q=%25");

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task List_finds_unassigned_leads()
    {
        await Post(NewLead("a@one.example", owner: "Ana Ruiz"));
        await Post(NewLead("b@two.example"));

        var result = await List("owner=Unassigned");

        Assert.Equal("b@two.example", Assert.Single(result.Items).Email);
    }

    [Fact]
    public async Task List_pages_newest_first_and_keeps_the_full_total()
    {
        await Post(NewLead("first@one.example"));
        await Post(NewLead("second@two.example"));
        await Post(NewLead("third@three.example"));

        var firstPage = await List("page=1&pageSize=2");
        var secondPage = await List("page=2&pageSize=2");

        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(["third@three.example", "second@two.example"], firstPage.Items.Select(l => l.Email));
        Assert.Equal("first@one.example", Assert.Single(secondPage.Items).Email);
    }

    private Task<HttpResponseMessage> Post(object lead) => _client.PostAsJsonAsync("/api/leads", lead);

    private async Task<LeadListResponse> List(string query) =>
        (await _client.GetFromJsonAsync<LeadListResponse>($"/api/leads?{query}", Json))!;

    private static object NewLead(
        string email,
        string firstName = "Test",
        string lastName = "Lead",
        string company = "Test Co",
        string status = "New",
        string? industry = null,
        string? owner = null) =>
        new { firstName, lastName, email, company, status, source = "Website", industry, owner };
}
