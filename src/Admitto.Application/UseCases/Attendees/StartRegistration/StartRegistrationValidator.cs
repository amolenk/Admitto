using Amolenk.Admitto.Domain;

namespace Amolenk.Admitto.Application.UseCases.Attendees.StartRegistration;

public class StartRegistrationValidator : AbstractValidator<StartRegistrationRequest>
{
    public StartRegistrationValidator()
    {
        RuleFor(x => x.Email)
            .NotNull().WithMessage(ErrorMessage.Attendee.Email.IsRequired)
            .EmailAddress().WithMessage(ErrorMessage.Attendee.Email.MustBeValid);

        RuleFor(x => x.FirstName)
            .NotNull().WithMessage(ErrorMessage.Attendee.FirstName.IsRequired)
            .MinimumLength(2).WithMessage(ErrorMessage.Attendee.FirstName.MustBeMin2Length)
            .MaximumLength(50).WithMessage(ErrorMessage.Attendee.FirstName.MustBeMax50Length);

        RuleFor(x => x.LastName)
            .NotNull().WithMessage(ErrorMessage.Attendee.LastName.IsRequired)
            .MinimumLength(2).WithMessage(ErrorMessage.Attendee.LastName.MustBeMin2Length)
            .MaximumLength(50).WithMessage(ErrorMessage.Attendee.LastName.MustBeMax50Length);

        // RuleFor(x => x.Details)
        //     .NotNull().WithMessage(ErrorMessage.Attendee.Details.AreRequired);
        
        RuleForEach(x => x.AdditionalDetails)
            .ChildRules(detail =>
            {
                detail.RuleFor(x => x.Name)
                    .NotNull().WithMessage(ErrorMessage.Attendee.Details.Key.IsRequired)
                    .NotEmpty().WithMessage(ErrorMessage.Attendee.Details.Key.MustNotBeEmpty)
                    .MaximumLength(50).WithMessage(ErrorMessage.Attendee.Details.Key.MustBeMax50Length);

                detail.RuleFor(x => x.Value)
                    .NotNull().WithMessage(ErrorMessage.Attendee.Details.Value.IsRequired)
                    .MaximumLength(50).WithMessage(ErrorMessage.Attendee.Details.Value.MustBeMax50Length);
            });

        RuleFor(x => x.Tickets)
            .NotNull().WithMessage(ErrorMessage.AttendeeRegistration.Tickets.AreRequired)
            .NotEmpty().WithMessage(ErrorMessage.AttendeeRegistration.Tickets.MustNotBeEmpty);
        
        RuleForEach(x => x.Tickets)
            .ChildRules(ticketType =>
            {
                ticketType.RuleFor(x => x.TicketTypeSlug)
                    .NotEmpty().WithMessage(ErrorMessage.AttendeeRegistration.Tickets.TicketType.MustNotBeEmpty);

                ticketType.RuleFor(x => x.Quantity)
                    .NotEmpty().WithMessage(ErrorMessage.AttendeeRegistration.Tickets.Quantity.MustNotBeEmpty)
                    .GreaterThan(0)
                    .WithMessage(ErrorMessage.AttendeeRegistration.Tickets.Quantity.MustBeGreaterThanZero);
            });
    }
}