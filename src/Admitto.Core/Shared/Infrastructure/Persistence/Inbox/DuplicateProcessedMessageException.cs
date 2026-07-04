namespace Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Inbox;

public sealed class DuplicateProcessedMessageException(Exception innerException)
    : Exception("The message has already been processed by this handler.", innerException);
