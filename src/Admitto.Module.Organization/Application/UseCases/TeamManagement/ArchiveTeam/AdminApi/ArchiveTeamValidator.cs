using FluentValidation;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.ArchiveTeam.AdminApi;

public sealed class ArchiveTeamValidator : AbstractValidator<ArchiveTeamHttpRequest>
{
    public ArchiveTeamValidator()
    {
        RuleFor(x => x.ExpectedVersion)
            .NotNull();
    }
}
