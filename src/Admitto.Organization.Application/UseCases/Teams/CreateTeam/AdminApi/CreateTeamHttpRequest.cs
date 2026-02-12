using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Application.UseCases.Teams.CreateTeam.AdminApi;

public sealed record CreateTeamHttpRequest(
    string Slug,
    string Name,
    string EmailAddress)
{
    internal CreateTeamCommand ToCommand()
        => new(
            Shared.Kernel.ValueObjects.Slug.From(Slug),
            DisplayName.From(Name),
            Shared.Kernel.ValueObjects.EmailAddress.From(EmailAddress));
}