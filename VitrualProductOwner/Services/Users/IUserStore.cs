using BlazorApp1.Models;

namespace BlazorApp1.Services.Users;

public interface IUserStore
{
    Task<User?> FindByUsernameAsync(string username, CancellationToken ct = default);
    Task<bool> ValidatePasswordAsync(User user, string password, CancellationToken ct = default);
}
