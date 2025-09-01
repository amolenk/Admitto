using Amolenk.Admitto.Application.Common.Validation;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.CreateTicketedEvent;

public class CreateTicketedEventValidator : AbstractValidator<CreateTicketedEventRequest>
{
    public CreateTicketedEventValidator()
    {
        RuleFor(x => x.Slug)
            .NotNull()
            .MinimumLength(2)
            .MaximumLength(32)
            .Slug();

        RuleFor(x => x.Name)
            .NotNull()
            .MinimumLength(2)
            .MaximumLength(50);

        RuleFor(x => x.StartTime)
            .NotEmpty();

        RuleFor(x => x.EndTime)
            .NotEmpty()
            .GreaterThan(x => x.StartTime);

        // RuleForEach(x => x.TicketTypes)
        //     .ChildRules(ticketType =>
        //     {
        //         ticketType.RuleFor(t => t.Name)
        //             .NotNull().WithMessage("Ticket type name is required.")
        //             .MinimumLength(2).WithMessage("Ticket type name must be at least 2 characters long.")
        //             .MaximumLength(50).WithMessage("Ticket type name must be 50 characters or less.")
        //             .OverridePropertyName("name");
        //
        //         ticketType.RuleFor(t => t.SlotName)
        //             .NotNull().WithMessage("Slot name is required.")
        //             .MinimumLength(2).WithMessage("Slot name must be at least 2 characters long.")
        //             .MaximumLength(50).WithMessage("Slot name must be 50 characters or less.")
        //             .OverridePropertyName("slotName");
        //
        //         ticketType.RuleFor(t => t.MaxCapacity)
        //             .NotEmpty().WithMessage("Max capacity is required.")
        //             .OverridePropertyName("maxCapacity");
        //     })
        //     .OverridePropertyName("ticketTypes");
        //

    }
}