using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UniversitySocial.Api.Data;

namespace UniversitySocial.Api.Tests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string TestKey = "integration-test-signing-key-32-bytes-minimum-value";
    private readonly string _databaseName = $"tests-{Guid.NewGuid()}";

    public CustomWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("Jwt__Key", TestKey);
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", "Host=localhost;Database=tests;Username=tests");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Jwt:Key"] = TestKey,
                ["Jwt:Issuer"] = "UniversitySocial",
                ["Jwt:Audience"] = "UniversitySocial.Client",
                ["ConnectionStrings:Default"] = "Host=localhost;Database=tests;Username=tests",
                ["ApplyMigrations"] = "false"
            }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }
}
