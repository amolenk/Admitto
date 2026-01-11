using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.Common.Data;

public static class ParticipantExtensions
{
    extension(IQueryable<Participant> participants)
    {
        public async ValueTask<Participant> GetAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var result = await participants
                .Where(e => e.Id == id)
                .FirstOrDefaultAsync(cancellationToken);

            return result ?? throw new ApplicationRuleException(ApplicationRuleError.Participant.NotFound);
        }

        public ValueTask<Participant> GetWithoutTrackingAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return participants.AsNoTracking().GetAsync(id, cancellationToken);
        }
    }
}