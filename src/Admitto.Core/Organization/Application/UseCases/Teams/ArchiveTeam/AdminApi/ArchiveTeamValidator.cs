using FluentValidation;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.Teams.ArchiveTeam.AdminApi;

public sealed class ArchiveTeamValidator : AbstractValidator<ArchiveTeamHttpRequest>
{
    public ArchiveTeamValidator()
    {
    }
}
