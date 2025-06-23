using System.ComponentModel.DataAnnotations;

namespace Amolenk.Admitto.Infrastructure.Auth;

public class AccessTokenOptions
{
    [Required] 
    public string TokenPath { get; init; } = null!;

    [Required] 
    public string ClientId { get; init; } = null!;

    [Required] 
    public string Username { get; init; } = null!;

    [Required] public string Password { get; init; } = null!;
}
