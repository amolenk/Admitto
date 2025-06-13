using System.Text.Json;
using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Domain.DomainEvents;

namespace Amolenk.Admitto.Infrastructure.Messaging;

public class OutboxMessage(Guid id, JsonDocument data, string type, bool priority)
{
    public Guid Id { get; private set; } = id;

    public JsonDocument Data { get; private set; } = data;

    public string Type { get; private set; } = type;

    public bool Priority { get; private set; } = priority;

    public static OutboxMessage FromDomainEvent(IDomainEvent domainEvent, bool priority = false)
    {
        var type = domainEvent.GetType().FullName!["Amolenk.Admitto.Domain.DomainEvents.".Length..];

        return new OutboxMessage(domainEvent.Id, SerializeData(domainEvent), type, priority);
    }

    public static OutboxMessage FromCommand(ICommand command, bool priority = false)
    {
        var type = command.GetType().FullName!["Amolenk.Admitto.Application.UseCases.".Length..];
        
        return new OutboxMessage(command.Id, SerializeData(command), type, priority);
    }

    private static JsonDocument SerializeData(object data)
    {
        return JsonSerializer.SerializeToDocument(data, JsonSerializerOptions.Web);
    }
}
