using Amolenk.Admitto.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Kernel.Entities;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Domain.Entities;

/// <summary>
/// Represents an organizing team in the system.
/// </summary>
public class Team : Aggregate<TeamId>
{
    private Team(
        TeamId id,
        Slug slug,
        TeamName name,
        EmailAddress email)
        : base(id)
    {
        Slug = slug;
        Name = name;
        Email = email;
    }

    public Slug Slug { get; private set; }
    public TeamName Name { get; private set; }
    public EmailAddress Email { get; private set; }

    public static Team Create(
        Slug slug,
        TeamName name,
        EmailAddress email) =>
        new(
            TeamId.New(),
            slug,
            name,
            email);
}