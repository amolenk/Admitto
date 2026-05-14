namespace Amolenk.Admitto.Core.Organization.Application.Bootstrap;

/// <summary>
/// Configuration options for the bootstrap admin initializer.
/// </summary>
public sealed class BootstrapAdminOptions
{
    public const string SectionName = "Organization:BootstrapAdmin";

    /// <summary>
    /// The email address of the bootstrap administrator.
    /// When configured, the initializer ensures a User with this email exists and has the Admin flag set.
    /// </summary>
    public string? EmailAddress { get; set; }
}
