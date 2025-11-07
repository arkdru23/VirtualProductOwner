using System.ComponentModel.DataAnnotations;

namespace BlazorApp1.Models;

public class LoginRequest
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Password { get; set; } = string.Empty;
}
