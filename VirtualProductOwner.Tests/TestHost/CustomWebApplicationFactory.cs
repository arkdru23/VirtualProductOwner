using System.Linq;
using BlazorApp1.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace VirtualProductOwner.Tests.TestHost;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Disable LLM in tests to avoid real HTTP calls
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Llm:Enabled"] = "false"
            });
        });

        // Enable detailed logging for debugging
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Warning);
        });

        builder.ConfigureServices(services =>
        {
            // Usuń istniejącą rejestrację DbContext (np. VPO-Dev)
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<StoryDbContext>));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            // Zarejestruj InMemory z unikalną nazwą, aby izolować testy
            var dbName = $"VPO-Test-{Guid.NewGuid()}";
            services.AddDbContext<StoryDbContext>(options =>
            {
                options.UseInMemoryDatabase(dbName);
            });

            // Remove problematic NavigationManager-dependent HttpClient registration
            // This is only needed for Blazor components, not for API endpoints
            var httpClientDescriptor = services.FirstOrDefault(
                d => d.ServiceType == typeof(HttpClient) && 
                     d.Lifetime == ServiceLifetime.Scoped);
            
            if (httpClientDescriptor is not null)
            {
                services.Remove(httpClientDescriptor);
            }

            // Zbuduj provider i upewnij się, że kontekst jest gotowy
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<StoryDbContext>();
            _ = db.Database.EnsureCreated();
        });
    }
}
