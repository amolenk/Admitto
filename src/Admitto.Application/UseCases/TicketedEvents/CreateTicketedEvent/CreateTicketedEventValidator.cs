using Amolenk.Admitto.Application.Common.Validation;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.CreateTicketedEvent;

public class CustomLanguageManager : FluentValidation.Resources.LanguageManager
{
    public CustomLanguageManager() 
    {
        AddTranslation("en", ValidationErrorCode.EventSlug.Required, "'{PropertyName}' is required.");
    }
}
public class CreateTicketedEventValidator : AbstractValidator<CreateTicketedEventRequest>
{
    public CreateTicketedEventValidator()
    {
        RuleFor(x => x.Slug)
            .NotNull().WithErrorCode(ValidationErrorCode.EventSlug.Required)//.WithMessage("Event slug is required.")
            .MinimumLength(2).WithMessage("Event slug must be at least 2 characters long.")
            .MaximumLength(32).WithMessage("Event slug must be 32 characters or less.")
            .Slug().WithMessage("Event slug must contain only lowercase letters, numbers, and hyphens (no leading, trailing, or consecutive hyphens).");

        RuleFor(x => x.Name)
            .NotNull().WithMessage("Event name is required.")
            .MinimumLength(2).WithMessage("Event name must be at least 2 characters long.")
            .MaximumLength(50).WithMessage("Event name must be 50 characters or less.")
            .OverridePropertyName("name");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("Start time is required.")
            .OverridePropertyName("startTime");

        RuleFor(x => x.EndTime)
            .NotEmpty().WithMessage("End time is required.")
            .GreaterThan(x => x.StartTime).WithMessage("End time must be after start time.")
            .OverridePropertyName("endTime");

        RuleFor(x => x.RegistrationStartTime)
            .NotEmpty().WithMessage("Registration start time is required.")
            .LessThan(x => x.StartTime).WithMessage("Registration start time must be before event start time.")
            .OverridePropertyName("registrationStartTime");

        RuleFor(x => x.RegistrationEndTime)
            .NotEmpty().WithMessage("Registration end time is required.")
            .GreaterThan(x => x.RegistrationStartTime).WithMessage("Registration end time must be after registration start time.")
            .LessThan(x => x.StartTime).WithMessage("Registration end time must be before event start time.")
            .OverridePropertyName("registrationEndTime");
        
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