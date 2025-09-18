namespace Amolenk.Admitto.Application.UseCases.Attendees.RegisterAttendee;

public class RegisterAttendeeValidator : AbstractValidator<RegisterAttendeeRequest>
{
    public RegisterAttendeeValidator()
    {
        RuleFor(x => x.Email).NotNull().EmailAddress();

        // TODO
    }
}

