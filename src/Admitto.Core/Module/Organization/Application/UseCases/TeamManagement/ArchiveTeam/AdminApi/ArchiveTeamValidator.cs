using FluentValidation;

namespace Amolenk.Admitto.Core.Module.Organization.Application.UseCases.TeamManagement.ArchiveTeam.AdminApi;

public sealed class ArchiveTeamValidator : AbstractValidator<ArchiveTeamHttpRequest>
{
    public ArchiveTeamValidator()
    {
    }
}
