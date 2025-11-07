using System.Security.Cryptography;
using System.Text;
using BlazorApp1.Models;

namespace BlazorApp1.Services.Users;

public class InMemoryUserStore : IUserStore
{
    private readonly Dictionary<string, User> _byUsername;

    public InMemoryUserStore()
    {
        var demoUser = new User
        {
            Id = "1",
            Username = "admin",
            DisplayName = "Administrator",
            PasswordHash = HashPassword("Pass123$")
        };
        _byUsername = new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase)
        {
            [demoUser.Username] = demoUser
        };
    }

    public Task<User?> FindByUsernameAsync(string username, CancellationToken ct = default)
    {
        _ = _byUsername.TryGetValue(username, out var user);
        return Task.FromResult(user);
    }

    public Task<bool> ValidatePasswordAsync(User user, string password, CancellationToken ct = default)
    {
        var hash = HashPassword(password);
        return Task.FromResult(string.Equals(hash, user.PasswordHash, StringComparison.Ordinal));
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }
}
