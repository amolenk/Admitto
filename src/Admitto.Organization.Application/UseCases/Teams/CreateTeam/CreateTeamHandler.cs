using Amolenk.Admitto.Organization.Application.Persistence;
using Amolenk.Admitto.Organization.Domain.Entities;
using Amolenk.Admitto.Shared.Application.Messaging;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Application.UseCases.Teams.CreateTeam;

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