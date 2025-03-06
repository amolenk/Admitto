using System.Text.Json;
using Amolenk.Admitto.Domain.DomainEvents;

namespace Amolenk.Admitto.Application.Common.DTOs;

public class OutboxMessageDto(Guid id, JsonDocument payload, string discriminator)
{
    public Guid Id { get; private set; } = id;

    public JsonDocument Payload { get; private set; } = payload;

    public string Discriminator { get; private set; } = discriminator;

    public static OutboxMessageDto FromDomainEvent(IDomainEvent domainEvent)
    {
        return new OutboxMessageDto(domainEvent.DomainEventId, SerializePayload(domainEvent), GetDiscriminator(domainEvent.GetType()));
    }

    public static OutboxMessageDto FromCommand(ICommand command)
    {
        return new OutboxMessageDto(command.CommandId, SerializePayload(command), GetDiscriminator(command.GetType()));
    }

    private static JsonDocument SerializePayload(object payload)
    {
        return JsonSerializer.SerializeToDocument(payload, JsonSerializerOptions.Web);
    }
    
    private static string GetDiscriminator(Type type) => $"{type.FullName!}, {type.Assembly.GetName().Name}";
}
