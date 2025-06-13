using System.ComponentModel.DataAnnotations;

namespace Amolenk.Admitto.Infrastructure.Auth;

public class AccessTokenOptions
{
    [Required] public string TokenPath { get; set; }

    [Required] public string ClientId { get; set; }

    [Required] public string Username { get; set; }

    [Required] public string Password { get; set; }
}
