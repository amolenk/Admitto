namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.AddTicketType;

public class AddTicketTypeValidator : AbstractValidator<AddTicketTypeRequest>
{
    public AddTicketTypeValidator()
    {
        RuleFor(x => x.Name)
            .NotNull().WithMessage("Ticket type name is required.")
            .MinimumLength(2).WithMessage("Ticket type name must be at least 2 characters long.")
            .MaximumLength(50).WithMessage("Ticket type name must be 50 characters or less.");

        RuleFor(x => x.SlotName)
            .NotNull().WithMessage("Slot name is required.")
            .MinimumLength(2).WithMessage("Slot name must be at least 2 characters long.")
            .MaximumLength(50).WithMessage("Slot name must be 50 characters or less.");

        RuleFor(x => x.MaxCapacity)
            .NotEmpty().WithMessage("Max capacity is required.");
    }
}