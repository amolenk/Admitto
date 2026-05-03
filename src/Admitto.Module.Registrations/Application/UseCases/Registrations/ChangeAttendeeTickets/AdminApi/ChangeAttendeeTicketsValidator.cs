using FluentValidation;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.ChangeAttendeeTickets.AdminApi;

public sealed class ChangeAttendeeTicketsValidator : AbstractValidator<ChangeAttendeeTicketsHttpRequest>
{
    public ChangeAttendeeTicketsValidator()
    {
        RuleFor(x => x.TicketTypeSlugs)
            .NotNull()
            .NotEmpty()
            .WithMessage("'TicketTypeSlugs' must contain at least one ticket type.");

        RuleForEach(x => x.TicketTypeSlugs)
            .NotEmpty()
            .WithMessage("Ticket type slugs must not be blank.");
    }
}
