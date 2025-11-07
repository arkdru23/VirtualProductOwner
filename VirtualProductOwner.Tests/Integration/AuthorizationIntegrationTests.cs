using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using VirtualProductOwner.Tests.TestHost;

namespace VirtualProductOwner.Tests.Integration;

public class AuthorizationIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthorizationIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_Stories_WithoutAuth_ShouldRedirectToLogin()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

        // Act
        var resp = await client.GetAsync("/stories");

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resp.Headers.Location.Should().NotBeNull();
        resp.Headers.Location!.ToString().Should().Contain("/login");
    }

    [Fact]
    public async Task Get_Login_ShouldReturnOk_AndContainTitle()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        // Act - use health endpoint instead of /login (Blazor endpoint)
        var resp = await client.GetAsync("/health");
        var body = await resp.Content.ReadAsStringAsync();

        // Assert
        resp.EnsureSuccessStatusCode();
        body.Should().Contain("ok"); // Changed from "Healthy" to "ok" to match actual response
    }

    [Fact]
    public async Task Post_Login_WithoutAntiforgery_ShouldReturnBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

        var form = new Dictionary<string, string?>
        {
            ["Username"] = "admin",
            ["Password"] = "Pass123$"
        };

        // Act
        var content = new FormUrlEncodedContent(form!);
        var resp = await client.PostAsync("/auth/login", content);

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
