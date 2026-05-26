using Amolenk.Admitto.Core.Email.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Inbox;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.HandleReconfirmAutoExpired.EventHandlers;

internal sealed class ReconfirmAutoExpiredIntegrationEventHandler(IRegistrationsWriteStore writeStore)
    : IIntegrationEventHandler<ReconfirmAutoExpiredIntegrationEvent>
{
    public async ValueTask HandleAsync(
        ReconfirmAutoExpiredIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var messageKey = integrationEvent.IntegrationEventId.ToString("N");
        var alreadyHandled = await writeStore.ProcessedMessages
            .AnyAsync(x => x.MessageKey == messageKey, cancellationToken);

        if (alreadyHandled)
            return;

        foreach (var registrationIdValue in integrationEvent.RegistrationIds)
        {
            var registrationId = RegistrationId.From(registrationIdValue);
            var registration = await writeStore.Registrations
                .FirstOrDefaultAsync(r => r.Id == registrationId, cancellationToken);

            if (registration is null
                || registration.Status != RegistrationStatus.Registered
                || registration.HasReconfirmed)
            {
                continue;
            }

            registration.Cancel(CancellationReason.ReconfirmAutoCancel);
        }

        writeStore.ProcessedMessages.Add(
            ProcessedMessage.Create(messageKey, DateTimeOffset.UtcNow));
    }
}
