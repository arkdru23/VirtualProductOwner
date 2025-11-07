using BlazorApp1.Services.Auth;
using BlazorApp1.Services.Generator;
using BlazorApp1.Services.Stories;
using BlazorApp1.Services.Users;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using BlazorApp1.Data;
using BlazorApp1.Endpoints;
using Microsoft.AspNetCore.Components;
using BlazorApp1.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/access-denied";

        // Security hardening
        options.Cookie.Name = "VPO.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax; // Consider Strict if you don't need cross-site auth flows

        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpClient();

// Default HttpClient for Blazor Server components with BaseAddress set to app base URI
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(sp.GetRequiredService<NavigationManager>().BaseUri)
});

// LLM options & services
builder.Services.Configure<BlazorApp1.Models.LlmOptions>(builder.Configuration.GetSection("Llm"));
builder.Services.AddHttpClient<BlazorApp1.Services.Llm.HttpLlmClient>();
builder.Services.AddScoped<BlazorApp1.Services.Llm.ILlmClient, BlazorApp1.Services.Llm.HttpLlmClient>();

// Azure DevOps integration
builder.Services.Configure<BlazorApp1.Models.AdoOptions>(builder.Configuration.GetSection("AzureDevOps"));
builder.Services.AddHttpClient<BlazorApp1.Services.Ado.AdoService>();
builder.Services.AddScoped<BlazorApp1.Services.Ado.IAdoService, BlazorApp1.Services.Ado.AdoService>();

// Content extraction
builder.Services.AddScoped<BlazorApp1.Services.Extraction.IContentExtractionService, BlazorApp1.Services.Extraction.BasicContentExtractionService>();

// Response compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// Health checks
builder.Services.AddHealthChecks();

// Antiforgery config
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

// Rate limiting (e.g., throttle login attempts per IP)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst
            }));

    options.AddPolicy("chat", httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst
            }));
});

// EF Core provider selection (InMemory or SQLite)
var persistenceProvider = builder.Configuration["Persistence"] ?? "EfInMemory";
builder.Services.AddDbContext<StoryDbContext>(options =>
{
    if (string.Equals(persistenceProvider, "EfSqlite", StringComparison.OrdinalIgnoreCase))
    {
        var cs = builder.Configuration.GetConnectionString("Default") ?? "Data Source=vpo.db";
        options.UseSqlite(cs);
    }
    else
    {
        options.UseInMemoryDatabase("VPO-Dev");
    }
});

// DI registrations
builder.Services.AddSingleton<IUserStore, InMemoryUserStore>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IStoryService, EfStoryService>();
builder.Services.AddSingleton<IStoryGeneratorService, StoryGeneratorService>();
builder.Services.AddScoped<BlazorApp1.Services.Context.IContextAssetService, BlazorApp1.Services.Context.ContextAssetService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    _ = app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    _ = app.UseHsts();
}

app.UseHttpsRedirection();

app.UseResponseCompression();

// Security headers
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "no-referrer";
    headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:; " +
        "font-src 'self'; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "frame-ancestors 'none'";
    await next();
});

app.UseRateLimiter();

// Map auth endpoints FIRST (including GET /login) before other middleware
app.MapAuthEndpoints();

app.UseAuthentication();
app.UseAuthorization();

// Add antiforgery middleware after authentication/authorization
app.UseAntiforgery();

// Map other endpoints (auth already mapped above)
app.MapStoriesEndpoints();
app.MapContextAssetEndpoints();
app.MapGenerationEndpoints();
app.MapConversationEndpoints();
app.MapApprovalEndpoints();
app.MapHealthEndpoints();

app.MapStaticAssets();

app.MapRazorComponents<BlazorApp1.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program { }
