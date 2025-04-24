namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.CreateTicketedEvent;

public class CreateTicketedEventValidator : AbstractValidator<CreateTicketedEventRequest>
{
    // TODO Add UnitTests. Maybe instantiate the Endpoint and use it to test the validator.
    
    public CreateTicketedEventValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Event name is required.")
            .MaximumLength(100).WithMessage("Event name must be less than 100 characters.");

        // RuleFor(x => x.StartDay)
        //     .NotEmpty().WithMessage("Start date is required.");
        //
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