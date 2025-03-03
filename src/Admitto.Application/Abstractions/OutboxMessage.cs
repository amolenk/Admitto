using System.Text.Json;
using Amolenk.Admitto.Domain.DomainEvents;

namespace Amolenk.Admitto.Application.Abstractions;

public class OutboxMessage(Guid id, JsonDocument payload, string discriminator)
{
    // private OutboxMessage(Guid id, JsonDocument payload, string discriminator)
    // {
    //     Id = id;
    //     Payload = payload;
    //     Discriminator = discriminator;
    // }

    public Guid Id { get; private set; } = id;

    public JsonDocument Payload { get; private set; } = payload;

    public string Discriminator { get; private set; } = discriminator;

    public static OutboxMessage FromDomainEvent(IDomainEvent domainEvent)
    {
        return new OutboxMessage(domainEvent.DomainEventId, SerializePayload(domainEvent), GetDiscriminator(domainEvent.GetType()));
    }

    public static OutboxMessage FromCommand(ICommand command)
    {
        return new OutboxMessage(command.CommandId, SerializePayload(command), GetDiscriminator(command.GetType()));
    }

    private static JsonDocument SerializePayload(object payload)
    {
        return JsonSerializer.SerializeToDocument(payload, JsonSerializerOptions.Web);
    }
    
    private static string GetDiscriminator(Type type) => $"{type.FullName!}, {type.Assembly.GetName().Name}";
}
