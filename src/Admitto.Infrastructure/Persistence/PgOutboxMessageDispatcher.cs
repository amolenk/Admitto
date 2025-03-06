using System.Text.Json;
using Amolenk.Admitto.Application.Common.DTOs;
using Microsoft.Extensions.Logging;
using Npgsql.Replication;
using Npgsql.Replication.PgOutput;
using Npgsql.Replication.PgOutput.Messages;

namespace Amolenk.Admitto.Infrastructure.Persistence;

public class PgOutboxMessageDispatcher(string connectionString, ILogger<PgOutboxMessageDispatcher> logger)
{
    public const string SlotName = "outbox_slot";
    public const string PublicationName = "outbox_pub";
    
    public async Task ExecuteAsync(Func<OutboxMessageDto, CancellationToken, ValueTask> messageHandler, 
        CancellationToken cancellationToken)
    {
        await using var connection = new LogicalReplicationConnection(connectionString);
        await connection.Open(cancellationToken);

        var slot = new PgOutputReplicationSlot(SlotName);
        var options = new PgOutputReplicationOptions(PublicationName, PgOutputProtocolVersion.V4);
        
        await foreach (var message in connection.StartReplication(slot, options, cancellationToken))
        {
            if (message is InsertMessage insertMessage)
            {
                await HandleMessageAsync(insertMessage, messageHandler, cancellationToken);
            }

            connection.SetReplicationStatus(message.WalEnd);
        }
    }
    
     private async Task HandleMessageAsync(InsertMessage message, 
         Func<OutboxMessageDto, CancellationToken, ValueTask> messageHandler, CancellationToken cancellationToken)
     {
         const string idColumn = "id";
         const string discriminatorColumn = "discriminator";
         const string payloadColumn = "payload";

         var id = Guid.Empty;
         var discriminator = string.Empty;
         JsonDocument payload = null!;
         
         await foreach (var value in message.NewRow)
         {
             switch (value.GetFieldName())
             {
                 case idColumn:
                     id = Guid.Parse(await value.GetTextReader().ReadToEndAsync(cancellationToken));
                     break;
                 case discriminatorColumn:
                     discriminator = await value.GetTextReader().ReadToEndAsync(cancellationToken);
                     break;
                 case payloadColumn:
                     payload = await JsonDocument.ParseAsync(value.GetStream(),
                         cancellationToken: cancellationToken);
                     break;
             }
         }
         
         try
         {
             var outboxMessage = new OutboxMessageDto(id, payload, discriminator);
             await messageHandler(outboxMessage, cancellationToken);
         }
         catch (Exception e)
         {
             logger.LogError(e, "Failed to handle outbox message");
         }
     }
}
