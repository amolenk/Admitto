namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.UpdateTicketType;

public class UpdateTicketTypeValidator : AbstractValidator<UpdateTicketTypeRequest>
{
    public UpdateTicketTypeValidator()
    {
        RuleFor(x => x.MaxCapacity)
            .GreaterThanOrEqualTo(0);
    }
}