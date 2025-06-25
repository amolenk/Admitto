# Exactly-Once Message Processing

This implementation provides exactly-once message processing capabilities for command and domain event handlers in the Admitto application.

## Overview

Exactly-once processing ensures that messages are processed only once, even if they are delivered multiple times due to network issues, retries, or other scenarios. This is critical for operations that should not be duplicated, such as financial transactions, user registrations, or critical business logic.

## How to Use

### 1. Opt-in to Exactly-Once Processing

To enable exactly-once processing for a handler, implement the `IProcessMessagesExactlyOnce` marker interface:

```csharp
public class ReserveTicketsHandler(IDomainContext context, IMessageOutbox messageOutbox)
    : ICommandHandler<ReserveTicketsCommand>, IProcessMessagesExactlyOnce
{
    public async ValueTask HandleAsync(ReserveTicketsCommand command, CancellationToken cancellationToken)
    {
        // Your handler logic here
        // This will be executed exactly once per unique message
    }
}
```

### 2. For Domain Event Handlers

Domain event handlers can also implement exactly-once processing:

```csharp
public class TeamMemberAddedDomainEventHandler(ConfigureTeamUserHandler configureTeamUserHandler)
    : IEventualDomainEventHandler<TeamMemberAddedDomainEvent>, IProcessMessagesExactlyOnce
{
    public ValueTask HandleAsync(TeamMemberAddedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        // This will be executed exactly once per unique domain event
        return configureTeamUserHandler.HandleAsync(command, cancellationToken);
    }
}
```

## How It Works

1. **Message ID Tracking**: Each message is assigned a unique ID when it enters the system
2. **Database Check**: Before processing, the system checks if the message ID exists in the `processed_messages` table
3. **Atomic Operation**: If the message hasn't been processed, it's added to the processed messages table within the same database transaction as the handler's business logic
4. **Constraint Protection**: The database's unique constraint on message IDs prevents duplicate processing, even in race conditions
5. **Graceful Handling**: If a duplicate is detected, the system gracefully skips processing without errors

## Database Schema

The exactly-once processing uses a `processed_messages` table:

```sql
CREATE TABLE processed_messages (
    message_id UUID PRIMARY KEY,
    processed_at TIMESTAMP WITH TIME ZONE NOT NULL
);

CREATE INDEX ix_processed_messages_processed_at ON processed_messages (processed_at);
```

## Performance Considerations

- **Minimal Overhead**: Only handlers that implement `IProcessMessagesExactlyOnce` incur the tracking overhead
- **Database Constraint**: Uses database-level constraints for atomic duplicate detection
- **Index Optimization**: Includes an index on `processed_at` for potential cleanup operations

## When to Use

Use exactly-once processing for handlers that:

- ✅ Modify critical business data
- ✅ Perform financial transactions
- ✅ Send emails or notifications
- ✅ Integrate with external systems
- ✅ Cannot safely be executed multiple times

Avoid for handlers that:

- ❌ Are purely read-only
- ❌ Are naturally idempotent
- ❌ Have very high message volumes where the overhead isn't justified

## Testing

The implementation includes comprehensive tests:

- Unit tests for the `ExactlyOnceProcessor` 
- Integration tests for the complete message processing flow
- Tests for constraint violation handling and race conditions

## Migration

When deploying this feature, run the database migration to create the `processed_messages` table:

```bash
dotnet ef database update
```

The migration file is located at:
`src/Admitto.Infrastructure/Persistence/Migrations/20250101000000_AddProcessedMessagesTable.cs`