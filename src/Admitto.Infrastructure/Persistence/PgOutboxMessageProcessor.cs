using System.Text.Json;
using Amolenk.Admitto.Application.Common.Abstractions;
using Microsoft.Extensions.Logging;
using Npgsql.Replication;
using Npgsql.Replication.PgOutput;
using Npgsql.Replication.PgOutput.Messages;

namespace Amolenk.Admitto.Infrastructure.Persistence;

public class PgOutboxMessageProcessor(string connectionString, ILogger<PgOutboxMessageProcessor> logger)
{
    public const string SlotName = "outbox_slot";
    public const string PublicationName = "outbox_pub";
    
    public async ValueTask ProcessMessagesAsync(Func<OutboxMessage, CancellationToken, ValueTask> messageHandler, 
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
        Func<OutboxMessage, CancellationToken, ValueTask> messageHandler, CancellationToken cancellationToken)
    {
        const string idColumn = "id";
        const string typeColumn = "type";
        const string dataColumn = "data";
        const string priorityColumn = "priority";

        var id = Guid.Empty;
        var type = string.Empty;
        JsonDocument data = null!;
        var priority = false;

        await foreach (var value in message.NewRow)
        {
            switch (value.GetFieldName())
            {
                case idColumn:
                    id = Guid.Parse(await value.GetTextReader().ReadToEndAsync(cancellationToken));
                    break;
                case typeColumn:
                    type = await value.GetTextReader().ReadToEndAsync(cancellationToken);
                    break;
                case dataColumn:
                    data = await JsonDocument.ParseAsync(value.GetStream(),
                        cancellationToken: cancellationToken);
                    break;
                case priorityColumn:
                    priority = await value.GetTextReader().ReadToEndAsync(cancellationToken) == "t";
                    break;
            }
        }

        try
        {
            logger.LogDebug("Received priority outbox message {Id} of type {Type}", id, type);

            var outboxMessage = new OutboxMessage(id, data, type, priority);
            await messageHandler(outboxMessage, cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to handle outbox message");
        }
    }
}
