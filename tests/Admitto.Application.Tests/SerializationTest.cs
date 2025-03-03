using System.Text.Json;
using Amolenk.Admitto.Application.Abstractions;
using Amolenk.Admitto.Application.Features.TicketedEvents.CreateTicketedEvent;
using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Infrastructure.Persistence;
using Microsoft.Azure.Cosmos;

namespace Amolenk.Admitto.Application.Tests;

public class SerializationTest
{
    [Test]
    public async Task Test()
    {
        var ticketedEvent = TicketedEvent.Create(
            "MyEvent",
            new DateOnly(2025, 4, 15),
            new DateOnly(2025, 4, 15),
            DateTime.Now,
            DateTime.Now.AddMonths(1));
        
        ticketedEvent.AddTicketType(TicketType.Create(
            "VIP",
            new DateTime(2025, 4, 15, 9, 0, 0),
            new DateTime(2025, 4, 15, 17, 0, 0),
            100));

        var result = JsonSerializer.Serialize(ticketedEvent, JsonSerializerOptions.Web);

        Console.WriteLine(result);

        var deserialized = JsonSerializer.Deserialize<TicketedEvent>(result, 
            JsonSerializerOptions.Web);
        
        await Assert.That(deserialized.Name).IsEqualTo(ticketedEvent.Name);
        await Assert.That(deserialized.TicketTypes.Count).IsEqualTo(ticketedEvent.TicketTypes.Count);
        
    }
    
    [Test]
    public async Task OutboxTest()
    {
        var command = new CreateTicketedEventCommand(
            "MyEvent",
            new DateOnly(2025, 4, 15),
            new DateOnly(2025, 4, 15),
            DateTime.Now,
            DateTime.Now.AddMonths(1), []);

        var message = OutboxMessage.FromCommand(command);

        var document = new CosmosDocument<OutboxMessage>
        {
            Id = Guid.NewGuid().ToString(),
            PartitionKey = "Outbox",
            Payload = message,
            Discriminator = nameof(OutboxMessage)
        };
        
        var result = JsonSerializer.Serialize(document, JsonSerializerOptions.Web);

        Console.WriteLine(result);

        var deserialized = JsonSerializer.Deserialize<CosmosDocument<OutboxMessage>>(result, 
            JsonSerializerOptions.Web);

        var jsonElement = (JsonElement)deserialized.Payload.Payload;

        var bodyType = Type.GetType(deserialized.Payload.Discriminator);
        
        var messageBody = jsonElement.Deserialize(bodyType, JsonSerializerOptions.Web);

        if (messageBody is ICommand)
        {
            Console.WriteLine("Got a command");
        }
        
        await Assert.That(((CreateTicketedEventCommand)messageBody).Name).IsEqualTo(command.Name);
    }
}