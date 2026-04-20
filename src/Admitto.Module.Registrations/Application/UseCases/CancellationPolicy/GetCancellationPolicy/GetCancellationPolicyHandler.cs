using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.CancellationPolicy.GetCancellationPolicy;

internal sealed class GetCancellationPolicyHandler(IRegistrationsWriteStore writeStore)
    : IQueryHandler<GetCancellationPolicyQuery, CancellationPolicyDto?>
{
    public async ValueTask<CancellationPolicyDto?> HandleAsync(
        GetCancellationPolicyQuery query,
        CancellationToken cancellationToken)
    {
        var policy = await writeStore.CancellationPolicies
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == query.EventId, cancellationToken);

        if (policy is null)
        {
            return null;
        }

        return new CancellationPolicyDto(policy.LateCancellationCutoff);
    }
}
