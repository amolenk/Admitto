using Amolenk.Admitto.Application.Common.Validation;
using Amolenk.Admitto.Domain;

namespace Amolenk.Admitto.Application.UseCases.Attendees.StartRegistration;

public class StartRegistrationValidator : AbstractValidator<StartRegistrationRequest>
{
    public StartRegistrationValidator()
    {
        RuleFor(x => x.Email)
            .NotNull()
            .EmailAddress();

        RuleFor(x => x.FirstName)
            .NotNull()
            .MinimumLength(2)
            .MaximumLength(50);

        RuleFor(x => x.LastName)
            .NotNull()
            .MinimumLength(2)
            .MaximumLength(50);

        RuleForEach(x => x.AdditionalDetails)
            .ChildRules(detail =>
            {
                detail.RuleFor(x => x.Name)
                    .NotNull()
                    .NotEmpty()
                    .MaximumLength(50);

                detail.RuleFor(x => x.Value)
                    .NotNull()
                    .MaximumLength(50);
            });

        RuleFor(x => x.Tickets)
            .NotNull()
            .NotEmpty();
        
        RuleForEach(x => x.Tickets)
            .ChildRules(ticketType =>
            {
                ticketType.RuleFor(x => x.TicketTypeSlug)
                    .NotEmpty()
                    .Slug();

                ticketType.RuleFor(x => x.Quantity)
                    .NotEmpty()
                    .GreaterThan(0);
            });
    }
}