using System.Text.Json;
using Amolenk.Admitto.Domain.DomainEvents;

namespace Amolenk.Admitto.Application.Common.Abstractions;

public class Message(Guid id, JsonDocument data, string type)
{
    public Guid Id { get; private set; } = id;

    public JsonDocument Data { get; private set; } = data;

    public string Type { get; private set; } = type;

    public static Message FromDomainEvent(DomainEvent domainEvent)
    {
        var type = domainEvent.GetType().FullName!["Amolenk.Admitto.Domain.DomainEvents.".Length..];

        return new Message(domainEvent.DomainEventId, SerializeData(domainEvent), type);
    }

    public static Message FromCommand(Command command)
    {
        var type = command.GetType().FullName!["Amolenk.Admitto.Application.UseCases.".Length..];
        
        return new Message(command.CommandId, SerializeData(command), type);
    }

    private static JsonDocument SerializeData(object data)
    {
        return JsonSerializer.SerializeToDocument(data, JsonSerializerOptions.Web);
    }
}
