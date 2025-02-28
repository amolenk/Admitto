using System.Text.Json.Serialization;
using Amolenk.Admitto.Domain.DomainEvents;

namespace Amolenk.Admitto.Application.Abstractions;

public class OutboxMessage
{
    [JsonConstructor]
    private OutboxMessage(Guid id, object body, string discriminator)
    {
        Id = id;
        Body = body;
        Discriminator = discriminator;
    }

    [JsonPropertyName("id")]
    public Guid Id { get; private set; }

    [JsonPropertyName("body")]
    public object Body { get; private set; }

    [JsonPropertyName("$type")] 
    public string Discriminator { get; private set; }

    public static OutboxMessage FromDomainEvent(IDomainEvent domainEvent)
    {
        return new OutboxMessage(domainEvent.DomainEventId, domainEvent, GetDiscriminator(domainEvent.GetType()));
    }

    public static OutboxMessage FromCommand(ICommand command)
    {
        return new OutboxMessage(command.CommandId, command, GetDiscriminator(command.GetType()));
    }
    
    private static string GetDiscriminator(Type type) => $"{type.FullName!}, {type.Assembly.GetName().Name}";
}
