using Amolenk.Admitto.Application.Common.Validation;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Teams.CreateTeam;

public class CreateTeamValidator : AbstractValidator<CreateTeamRequest>
{
    public CreateTeamValidator()
    {
        // TODO Use ErrorMessage class

        RuleFor(x => x.Slug)
            .NotNull()//.WithMessage("Team slug is required.")
            .MinimumLength(2)//.WithMessage("Team slug must be at least 2 characters long.")
            .MaximumLength(32)//.WithMessage("Team slug must be 32 characters or less.")
            .Slug().WithMessage("Team slug must contain only lowercase letters, numbers, and hyphens (no leading, trailing, or consecutive hyphens).");

        RuleFor(x => x.Name)
            .NotNull()//.WithMessage("Team name is required.")
            .MinimumLength(2)//.WithMessage("Team name must be at least 2 characters long.")
            .MaximumLength(50);//.WithMessage("Team name must be 50 characters or less.");

        // TODO Move
        // RuleForEach(x => x.Members)
        //     .ChildRules(member =>
        //     {
        //         member.RuleFor(m => m.Email)
        //             .NotNull().WithMessage("Member email is required.")
        //             .EmailAddress().WithMessage("Member email must be a valid email address.");
        //
        //         member.RuleFor(m => m.Role)
        //             .Must(TeamMemberRole.IsValid)
        //             .WithMessage($"Member role must be one of {string.Join(", ", TeamMemberRole.ValidRoles)}.");
        //     });

        // TODO Fix validation
        //
//         RuleFor(x => x.EmailSettings)
//             .NotNull();
// //            .WithMessage("Email settings are required.");
//
//         // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
//         When(x => x.EmailSettings is not null, () =>
//         {
//             RuleFor(x => x.EmailSettings.SenderEmail)
//                 .NotNull()//.WithMessage("Sender email is required.")
//                 .EmailAddress();//.WithMessage("Sender email must be a valid email address.");
//
//             RuleFor(x => x.EmailSettings.SmtpServer)
//                 .NotEmpty()//.WithMessage("SMTP server must not be empty.")
//                 .MaximumLength(20);//.WithMessage("SMTP server must be 50 characters or less.");
//
//             RuleFor(x => x.EmailSettings.SmtpPort)
//                 .InclusiveBetween(1, 65535).WithMessage("SMTP port must be between 1 and 65535.");
//         });
    }
}