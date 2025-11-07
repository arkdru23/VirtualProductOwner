using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using VirtualProductOwner.Tests.TestHost;

namespace VirtualProductOwner.Tests.Integration;

public class ConversationEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ConversationEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Conversation_Get_And_Post_ShouldWork_WithAuth_AndCsrf()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        // 1) Get CSRF token to login and set cookie
        var loginTokenResp = await client.GetAsync("/antiforgery/login-token");
        loginTokenResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginTokenJson = await loginTokenResp.Content.ReadFromJsonAsync<TokenDto>();
        loginTokenJson.Should().NotBeNull();
        loginTokenJson!.token.Should().NotBeNullOrWhiteSpace();

        // 2) Login
        var form = new Dictionary<string, string?>
        {
            ["Username"] = "admin",
            ["Password"] = "Pass123$",
            ["__RequestVerificationToken"] = loginTokenJson!.token
        };
        var loginResp = await client.PostAsync("/auth/login", new FormUrlEncodedContent(form!));
        loginResp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        loginResp.Headers.Location!.ToString().Should().Contain("/stories");

        // 3) Get CSRF token for JSON endpoints
        var apiToken = await client.GetFromJsonAsync<TokenDto>("/api/antiforgery/token");
        apiToken.Should().NotBeNull();
        apiToken!.token.Should().NotBeNullOrWhiteSpace();
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", apiToken.token);

        // 4) Create story
        var create = new CreateStoryDto { Title = "Chat test", Description = "Desc", Points = 3 };
        var createResp = await client.PostAsJsonAsync("/api/stories/", create);
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<StoryCreatedDto>();
        created.Should().NotBeNull();
        created!.Id.Should().NotBe(Guid.Empty);

        // 5) GET conversation history (should be empty)
        var historyResp = await client.GetAsync($"/api/stories/{created.Id}/conversation/");
        historyResp.EnsureSuccessStatusCode();
        var history = await historyResp.Content.ReadFromJsonAsync<HistoryDto>();
        history.Should().NotBeNull();
        history!.messages.Should().NotBeNull();

        // 6) POST message WITHOUT CSRF (remove header) -> 400
        var clientNoCsrf = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        // use already logged in cookie by re-login quickly to second client:
        var lt2 = await clientNoCsrf.GetFromJsonAsync<TokenDto>("/antiforgery/login-token");
        lt2.Should().NotBeNull();
        var loginResp2 = await clientNoCsrf.PostAsync("/auth/login", new FormUrlEncodedContent(new Dictionary<string, string?>
        {
            ["Username"]="admin",["Password"]="Pass123$",["__RequestVerificationToken"]=lt2!.token
        }!));
        loginResp2.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var noCsrfPost = await clientNoCsrf.PostAsJsonAsync($"/api/stories/{created.Id}/conversation/messages",
            new { content = "Refine this title", assetIds = Array.Empty<Guid>() });
        noCsrfPost.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 7) POST message WITH CSRF -> 200
        var post = await client.PostAsJsonAsync($"/api/stories/{created.Id}/conversation/messages",
            new { content = "Make acceptance criteria clearer", assetIds = Array.Empty<Guid>() });
        post.StatusCode.Should().Be(HttpStatusCode.OK);
        var chat = await post.Content.ReadFromJsonAsync<ChatResponseDto>();
        chat.Should().NotBeNull();
        chat!.messages.Should().NotBeNull();
    }

    private record TokenDto(string token);
    private record CreateStoryDto
    {
        public string Title { get; init; } = "";
        public string Description { get; init; } = "";
        public int Points { get; init; } = 3;
    }
    private record StoryCreatedDto(Guid Id, string Title, string Description, int Points);
    private record HistoryDto(List<ChatMsg> messages);
    private record ChatMsg(string Role, string Content, DateTime CreatedAt);
    private record ChatResponseDto(List<ChatMsg> messages, RefinedSuggestion? suggestion);
    private record RefinedSuggestion(string Title, string Description, int Points, string? AcceptanceCriteria);
}
