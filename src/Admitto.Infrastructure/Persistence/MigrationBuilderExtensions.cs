using Microsoft.EntityFrameworkCore.Migrations;

namespace Amolenk.Admitto.Infrastructure.Persistence;

public static class MigrationBuilderExtensions
{
    public static void AddOutboxReplication(this MigrationBuilder migrationBuilder)
    {
        // Note: The following SQL commands cannot run inside a transaction, so we suppress the transaction here.
        
        // Create a publication for the outbox table.
        // A publication is essentially a group of tables whose data changes are intended to be replicated through
        // logical replication.
        migrationBuilder.Sql($"CREATE PUBLICATION {PgOutboxMessageDispatcher.PublicationName} FOR TABLE outbox;",
            suppressTransaction: true);

        // Create a replication slot, which will hold the state of the replication stream.
        // When Admitto goes down, the slot persistently records the last data streamed to it, and allows resuming the
        // application at the point where it left off.
        migrationBuilder.Sql($"SELECT * FROM pg_create_logical_replication_slot('{PgOutboxMessageDispatcher.SlotName}', 'pgoutput');",
            suppressTransaction: true);
    }
}