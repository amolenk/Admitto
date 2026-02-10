using Amolenk.Admitto.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Shared.Kernel.ValueObjects;

public readonly record struct TicketTypeId : IGuidValueObject
{
    public Guid Value { get; }
    
    private TicketTypeId(Guid value) => Value = value;

    public static TicketTypeId New() => new(Guid.NewGuid());

    public static ValidationResult<TicketTypeId> TryFrom(Guid value)
        => GuidValueObject.TryFrom(value, v => new TicketTypeId(v), Errors.Empty);

    public static TicketTypeId From(Guid value)
        => GuidValueObject.TryFrom(value, v => new TicketTypeId(v), Errors.Empty).GetValueOrThrow();

    public override string ToString() => Value.ToString();

    private static class Errors
    {
        public static readonly Error Empty =
            new("ticket_type_id.empty", "Ticket type ID is required.");
    }
}