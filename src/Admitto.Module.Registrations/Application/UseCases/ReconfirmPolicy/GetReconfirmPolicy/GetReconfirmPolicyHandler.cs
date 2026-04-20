using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.ReconfirmPolicy.GetReconfirmPolicy;

internal sealed class GetReconfirmPolicyHandler(IRegistrationsWriteStore writeStore)
    : IQueryHandler<GetReconfirmPolicyQuery, ReconfirmPolicyDto?>
{
    public async ValueTask<ReconfirmPolicyDto?> HandleAsync(
        GetReconfirmPolicyQuery query,
        CancellationToken cancellationToken)
    {
        var policy = await writeStore.ReconfirmPolicies
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == query.EventId, cancellationToken);

        if (policy is null)
        {
            return null;
        }

        return new ReconfirmPolicyDto(policy.OpensAt, policy.ClosesAt, (int)policy.Cadence.TotalDays);
    }
}
