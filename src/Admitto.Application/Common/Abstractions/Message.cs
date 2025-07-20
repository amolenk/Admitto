using System.Text.Json;
using Amolenk.Admitto.Domain.DomainEvents;

namespace Amolenk.Admitto.Application.Common.Abstractions;

public class Message(Guid id, JsonDocument data, string type, bool priority)
{
    public Guid Id { get; private set; } = id;

    public JsonDocument Data { get; private set; } = data;

    public string Type { get; private set; } = type;

    public bool Priority { get; private set; } = priority;

    public static Message FromDomainEvent(IDomainEvent domainEvent, bool priority = false)
    {
        var type = domainEvent.GetType().FullName!["Amolenk.Admitto.Domain.DomainEvents.".Length..];

        return new Message(domainEvent.Id, SerializeData(domainEvent), type, priority);
    }

    public static Message FromCommand(Command command, bool priority = false)
    {
        var type = command.GetType().FullName!["Amolenk.Admitto.Application.UseCases.".Length..];
        
        return new Message(command.CommandId, SerializeData(command), type, priority);
    }

    private static JsonDocument SerializeData(object data)
    {
        return JsonSerializer.SerializeToDocument(data, JsonSerializerOptions.Web);
    }
}
