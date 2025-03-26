// using System.Text.Json;
// using Amolenk.Admitto.Application.UseCases.TicketedEvents.CreateTicketedEvent;
// using Amolenk.Admitto.Domain.DomainEvents;
// using Azure.Messaging;
// using Json.More;
//
// namespace Amolenk.Admitto.Application.Tests;
//
// [TestClass]
// public class CloudEventTests
// {
//     [TestMethod]
//     public async Task Test()
//     {
//         var command = new CreateTicketedEventCommand("Test", DateOnly.MinValue, DateOnly.MaxValue,
//             DateTime.UtcNow, DateTime.UtcNow, []);
//
//         var options = new JsonSerializerOptions
//         {
//             PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
//             PropertyNameCaseInsensitive = true
//         };
//
//         var dataJson = JsonSerializer.Serialize(command, options);
//
//         var cloudEvent = new CloudEvent("admitto", GetDataType(command), new BinaryData(dataJson),
//             "application/json")
//         {
//             Id = Guid.Empty.ToString()
//         };
//
//         var data = new BinaryData(cloudEvent);
//         
//         var receivedEvent = CloudEvent.Parse(data, true);
//         
//         Console.WriteLine("---");
//         Console.WriteLine(JsonSerializer.Serialize(receivedEvent));
//
//         // var json = receivedEvent.Data.ToObjectFromJson(typeof(CreateTicketedEventCommand));
//         
//         var command2 = (CreateTicketedEventCommand)JsonSerializer.Deserialize(receivedEvent.Data!.ToString(), typeof(CreateTicketedEventCommand), JsonSerializerOptions.Web);//.ToObjectFromJson<CreateTicketedEventCommand>(options);
//         
//         command2.Name.ShouldBe(command.Name);
//     }
//
//     private string GetDataType(object data)
//     {
//         var result = data.GetType().ToString();
//
//         if (data is IDomainEvent)
//         {
//             return result["Amolenk.Admitto.Domain.DomainEvents.".Length..];
//         }
//         else
//         {
//             return result["Amolenk.Admitto.Application.UseCases.".Length..];
//         }        
//     }
// }