using System.Security.Claims;
using BlazorApp1.Services.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BlazorApp1.Services.Auth;

public class AuthService(IUserStore userStore) : IAuthService
{
    private readonly IUserStore _userStore = userStore;

    public async Task<bool> SignInAsync(HttpContext context, string username, string password, CancellationToken ct = default)
    {
        var user = await _userStore.FindByUsernameAsync(username, ct);
        if (user is null)
        {
            return false;
        }

        var valid = await _userStore.ValidatePasswordAsync(user, password, ct);
        if (!valid)
        {
            return false;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.GivenName, user.DisplayName),
            new(ClaimTypes.Role, "User")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
        {
            IsPersistent = true,
            AllowRefresh = true
        });

        return true;
    }

    public Task SignOutAsync(HttpContext context, CancellationToken ct = default) => context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
}
