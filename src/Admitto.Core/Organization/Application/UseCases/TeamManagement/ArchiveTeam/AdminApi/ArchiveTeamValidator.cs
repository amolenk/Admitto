using FluentValidation;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamManagement.ArchiveTeam.AdminApi;

public sealed class ArchiveTeamValidator : AbstractValidator<ArchiveTeamHttpRequest>
{
    public ArchiveTeamValidator()
    {
    }
}
