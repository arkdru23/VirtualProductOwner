using System.Security.Claims;
using BlazorApp1.Models;
using BlazorApp1.Services.Auth;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BlazorApp1.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (HttpContext ctx, IAntiforgery antiforgery, IAuthService auth) =>
        {
            try { await antiforgery.ValidateRequestAsync(ctx); } catch (AntiforgeryValidationException) { return Results.BadRequest(); }

            var form = await ctx.Request.ReadFormAsync();
            var username = form["Username"].ToString();
            var password = form["Password"].ToString();

            var success = await auth.SignInAsync(ctx, username, password);
            return success ? Results.Redirect("/stories") : Results.Redirect("/login?error=1");
        })
        .RequireRateLimiting("login");

        app.MapPost("/logout", async (HttpContext ctx, IAntiforgery antiforgery) =>
        {
            try { await antiforgery.ValidateRequestAsync(ctx); } catch (AntiforgeryValidationException) { return Results.BadRequest(); }
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/");
        })
        .RequireAuthorization();

        app.MapGet("/antiforgery/login-token", (IAntiforgery af, HttpContext ctx) =>
        {
            var tokens = af.GetAndStoreTokens(ctx);
            return Results.Ok(new { token = tokens.RequestToken });
        });

        app.MapGet("/api/antiforgery/token", (IAntiforgery af, HttpContext ctx) =>
        {
            var tokens = af.GetAndStoreTokens(ctx);
            return Results.Ok(new { token = tokens.RequestToken });
        })
        .RequireAuthorization();

        return app;
    }
}
