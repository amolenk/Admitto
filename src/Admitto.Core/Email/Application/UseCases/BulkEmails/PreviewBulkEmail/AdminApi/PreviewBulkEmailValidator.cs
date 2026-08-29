using FluentValidation;
using Amolenk.Admitto.Core.Email.Application.Templating;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.PreviewBulkEmail.AdminApi;

public sealed class PreviewBulkEmailValidator : AbstractValidator<PreviewBulkEmailHttpRequest>
{
    public PreviewBulkEmailValidator()
    {
        RuleFor(x => x.EmailType)
            .Must(emailType => !string.Equals(
                emailType,
                BuiltInEmailTemplateNames.Reconfirmation,
                StringComparison.OrdinalIgnoreCase))
            .WithMessage("Reconfirmation email is sent by the hourly reconfirmation batch.")
            .When(request => request.EmailType is not null);

        RuleFor(x => x.AttendeeFilter).NotNull();
    }
}
