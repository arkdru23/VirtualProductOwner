namespace BlazorApp1.Models;

public class User
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Username { get; init; } = default!;
    public string PasswordHash { get; init; } = default!;
    public string DisplayName { get; init; } = default!;
}
