using Amolenk.Admitto.Domain;

namespace Amolenk.Admitto.Application.UseCases.Registrations.StartRegistration;

public class RegisterAttendeeValidator : AbstractValidator<RegisterAttendeeRequest>
{
    public RegisterAttendeeValidator()
    {
        RuleFor(x => x.TicketedEventId)
            .NotEmpty().WithMessage(ErrorMessage.TicketedEvent.Id.MustNotBeEmpty);

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

        RuleFor(x => x.Details)
            .NotNull().WithMessage(ErrorMessage.Attendee.Details.AreRequired);
        
        RuleForEach(x => x.Details)
            .ChildRules(detail =>
            {
                detail.RuleFor(x => x.Key)
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
                ticketType.RuleFor(x => x.Key)
                    .NotEmpty().WithMessage(ErrorMessage.AttendeeRegistration.Tickets.TicketType.MustNotBeEmpty);

                ticketType.RuleFor(x => x.Value)
                    .NotEmpty().WithMessage(ErrorMessage.AttendeeRegistration.Tickets.Quantity.MustNotBeEmpty)
                    .GreaterThan(0)
                    .WithMessage(ErrorMessage.AttendeeRegistration.Tickets.Quantity.MustBeGreaterThanZero);
            });
    }
}