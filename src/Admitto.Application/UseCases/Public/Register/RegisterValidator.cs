namespace Amolenk.Admitto.Application.UseCases.Public.Register;

public class RegisterValidator : AbstractValidator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MinimumLength(2).MaximumLength(50);
        RuleFor(x => x.LastName).NotEmpty().MinimumLength(2).MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.VerificationToken).NotEmpty();
        
        RuleFor(x => x.AdditionalDetails).NotNull();
        RuleForEach(x => x.AdditionalDetails)
            .ChildRules(details => {
                details.RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
                details.RuleFor(x => x.Value).MaximumLength(50);
            });

        RuleFor(x => x.RequestedTickets).NotNull();
        RuleForEach(x => x.RequestedTickets).NotEmpty().MaximumLength(100);
    }
}
