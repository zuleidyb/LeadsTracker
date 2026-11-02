using Xunit;

namespace LeadsTracker.E2E;

public sealed class StackFixture : IAsyncLifetime
{
    public string BaseUrl { get; } =
        Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:8088";

    public HttpClient Http { get; } = new();

    public async Task InitializeAsync()
    {
        Http.BaseAddress = new Uri(BaseUrl);
        var deadline = DateTime.UtcNow.AddSeconds(60);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await Http.GetAsync("/api/leads");
                if (response.IsSuccessStatusCode) return;
            }
            catch (HttpRequestException)
            {
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new TimeoutException($"The app at {BaseUrl} did not become ready within 60 seconds.");
    }

    public Task DisposeAsync()
    {
        Http.Dispose();
        return Task.CompletedTask;
    }
}
