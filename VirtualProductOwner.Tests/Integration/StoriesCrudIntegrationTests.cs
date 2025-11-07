using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BlazorApp1.Models;
using VirtualProductOwner.Tests.TestHost;

namespace VirtualProductOwner.Tests.Integration;

public class StoriesCrudIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public StoriesCrudIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Crud_AfterLogin_ShouldWork_WithAntiforgery()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        // 1) Get antiforgery token and cookie for login
        var loginTokenResp = await client.GetAsync("/antiforgery/login-token");
        loginTokenResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginTokenJson = await loginTokenResp.Content.ReadAsStringAsync();
        using var loginTokenDoc = JsonDocument.Parse(loginTokenJson);
        var loginToken = loginTokenDoc.RootElement.GetProperty("token").GetString();
        loginToken.Should().NotBeNullOrWhiteSpace();

        // 2) POST /login with token and credentials (cookie comes from previous GET)
        var form = new Dictionary<string, string?>
        {
            ["Username"] = "admin",
            ["Password"] = "Pass123$",
            ["__RequestVerificationToken"] = loginToken
        };

        var loginResp = await client.PostAsync("/auth/login", new FormUrlEncodedContent(form!));
        loginResp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        loginResp.Headers.Location!.ToString().Should().Contain("/stories");

        // 3) Get antiforgery token for API requests (authorized)
        var tokenResp = await client.GetAsync("/api/antiforgery/token");
        tokenResp.EnsureSuccessStatusCode();
        var tokenJson = await tokenResp.Content.ReadAsStringAsync();
        using var tokenDoc = JsonDocument.Parse(tokenJson);
        var apiToken = tokenDoc.RootElement.GetProperty("token").GetString();
        apiToken.Should().NotBeNullOrWhiteSpace();

        // Helper to add CSRF header
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", apiToken!);

        // 4) Create story
        var create = new CreateStoryRequest
        {
            Title = "Integration test story",
            Description = "Created via integration test",
            Points = 5
        };

        var createJson = new StringContent(JsonSerializer.Serialize(create), Encoding.UTF8, "application/json");
        var createResp = await client.PostAsync("/api/stories/", createJson);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdJson = await createResp.Content.ReadAsStringAsync();
        using var createdDoc = JsonDocument.Parse(createdJson);
        var id = createdDoc.RootElement.GetProperty("id").GetGuid();

        // 5) List stories
        var listResp = await client.GetAsync("/api/stories/");
        listResp.EnsureSuccessStatusCode();
        var listJson = await listResp.Content.ReadAsStringAsync();
        listJson.Should().Contain(id.ToString());

        // 6) Update story
        var update = new UpdateStoryRequest
        {
            Id = id,
            Title = "Updated title",
            Description = "Updated desc",
            Points = 8
        };
        var updateJson = new StringContent(JsonSerializer.Serialize(update), Encoding.UTF8, "application/json");
        var putResp = await client.PutAsync($"/api/stories/{id}", updateJson);
        putResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 7) Delete story
        var delResp = await client.DeleteAsync($"/api/stories/{id}");
        delResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 8) Verify deleted
        var listAfterDel = await client.GetAsync("/api/stories/");
        listAfterDel.EnsureSuccessStatusCode();
        var listAfterDelJson = await listAfterDel.Content.ReadAsStringAsync();
        listAfterDelJson.Should().NotContain(id.ToString());
    }
}
