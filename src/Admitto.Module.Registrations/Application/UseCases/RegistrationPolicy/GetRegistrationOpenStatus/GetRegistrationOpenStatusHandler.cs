using Amolenk.Admitto.Module.Email.Contracts;
using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.RegistrationPolicy.GetRegistrationOpenStatus;

internal sealed class GetRegistrationOpenStatusHandler(
    IRegistrationsWriteStore writeStore,
    IEventEmailFacade emailFacade)
    : IQueryHandler<GetRegistrationOpenStatusQuery, RegistrationOpenStatusDto>
{
    public async ValueTask<RegistrationOpenStatusDto> HandleAsync(
        GetRegistrationOpenStatusQuery query,
        CancellationToken cancellationToken)
    {
        var policy = await writeStore.EventRegistrationPolicies
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == query.EventId, cancellationToken);

        if (policy is null)
            throw new BusinessRuleViolationException(EventRegistrationPolicy.Errors.EventNotFound);

        var status = policy.RegistrationStatus;

        if (!policy.IsEventActive)
        {
            return new RegistrationOpenStatusDto(status, CanOpen: false, Reason: "event-not-active");
        }

        var emailConfigured = await emailFacade.IsEmailConfiguredAsync(query.EventId, cancellationToken);
        if (!emailConfigured)
        {
            return new RegistrationOpenStatusDto(status, CanOpen: false, Reason: "email-not-configured");
        }

        return new RegistrationOpenStatusDto(status, CanOpen: true, Reason: null);
    }
}
