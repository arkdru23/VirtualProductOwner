namespace BlazorApp1.Services.Auth;

public interface IAuthService
{
    Task<bool> SignInAsync(HttpContext context, string username, string password, CancellationToken ct = default);
    Task SignOutAsync(HttpContext context, CancellationToken ct = default);
}
