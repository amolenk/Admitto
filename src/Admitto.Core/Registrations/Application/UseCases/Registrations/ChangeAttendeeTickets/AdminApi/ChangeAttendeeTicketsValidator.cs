using FluentValidation;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ChangeAttendeeTickets.AdminApi;

public sealed class ChangeAttendeeTicketsValidator : AbstractValidator<ChangeAttendeeTicketsHttpRequest>
{
    public ChangeAttendeeTicketsValidator()
    {
        RuleFor(x => x.TicketTypeIds)
            .NotNull()
            .NotEmpty()
            .WithMessage("'TicketTypeIds' must contain at least one ticket type.");
    }
}
