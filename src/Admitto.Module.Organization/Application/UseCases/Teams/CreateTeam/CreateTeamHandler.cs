using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.Teams.CreateTeam;

internal sealed class CreateTeamHandler(IOrganizationWriteStore writeStore)
    : ICommandHandler<CreateTeamCommand>
{
    public async ValueTask HandleAsync(CreateTeamCommand command, CancellationToken cancellationToken)
    {
        var slug = Slug.From(command.Slug);
        var name = DisplayName.From(command.Name);
        var emailAddress = EmailAddress.From(command.EmailAddress);
        
        var team = Team.Create(slug, name, emailAddress);

        await writeStore.Teams.AddAsync(team, cancellationToken);
    }
}