namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.CreateTicketedEvent;

public class CreateTicketedEventValidator : AbstractValidator<CreateTicketedEventRequest>
{
    public CreateTicketedEventValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Event name is required.")
            .MaximumLength(100).WithMessage("Event name must be 100 characters or less.");

        RuleFor(x => x.StartDateTime)
            .NotEmpty().WithMessage("Start date is required.");
        
        // RuleFor(x => x.EndDay)
        //     .NotEmpty().WithMessage("End date is required.");
        //
        // RuleFor(x => x.RegistrationStartDay)
        //     .NotEmpty().WithMessage("Registration start date is required.");
        //
        // RuleFor(x => x.RegistrationEndDay)
        //     .NotEmpty().WithMessage("Registration end date is required.");

        RuleFor(x => x.TicketTypes)
            .NotEmpty().WithMessage("Registration end date is required.");
    }
}