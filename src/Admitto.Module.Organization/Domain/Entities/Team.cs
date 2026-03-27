using Amolenk.Admitto.Module.Shared.Kernel.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Domain.Entities;

/// <summary>
/// Represents an organizing team in the system.
/// </summary>
public class Team : Aggregate<TeamId>
{
    // ReSharper disable once UnusedMember.Local
    // Required for EF Core
    private Team()
    {
    }
    
    private Team(
        TeamId id,
        Slug slug,
        DisplayName name,
        EmailAddress emailAddress)
        : base(id)
    {
        Slug = slug;
        Name = name;
        EmailAddress = emailAddress;
    }

    public Slug Slug { get; private set; }
    public DisplayName Name { get; private set; }
    public EmailAddress EmailAddress { get; private set; }

    public static Team Create(
        Slug slug,
        DisplayName name,
        EmailAddress emailAddress) =>
        new(
            TeamId.New(),
            slug,
            name,
            emailAddress);

    public void ChangeSlug(Slug slug)
    {
        Slug = slug;
    }

    public void ChangeName(DisplayName name)
    {
        Name = name;
    }
    
    public void ChangeEmailAddress(EmailAddress emailAddress)
    {
        EmailAddress = emailAddress;
    }
}