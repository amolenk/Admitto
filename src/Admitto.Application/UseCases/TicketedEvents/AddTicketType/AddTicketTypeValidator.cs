namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.AddTicketType;

public class AddTicketTypeValidator : AbstractValidator<AddTicketTypeRequest>
{
    public AddTicketTypeValidator()
    {
        RuleFor(x => x.Name)
            .NotNull()
            .MinimumLength(2)
            .MaximumLength(50);

        RuleFor(x => x.SlotName)
            .NotNull()
            .MinimumLength(2)
            .MaximumLength(50);

        RuleFor(x => x.MaxCapacity)
            .NotEmpty();
    }
}