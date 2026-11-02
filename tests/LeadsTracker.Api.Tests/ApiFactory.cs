using LeadsTracker.Api.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LeadsTracker.Api.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string ConnectionString =
        "Host=localhost;Port=5435;Database=leadstracker_test;Username=leads_app;Password=leads_dev_password";

    public ApiFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", ConnectionString);
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LeadsDbContext>();
        await db.Database.MigrateAsync();
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;
}
