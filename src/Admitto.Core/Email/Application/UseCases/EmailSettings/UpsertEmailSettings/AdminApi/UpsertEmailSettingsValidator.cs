using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Validation;
using FluentValidation;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.UpsertEmailSettings.AdminApi;

public sealed class UpsertEmailSettingsValidator : AbstractValidator<UpsertEmailSettingsHttpRequest>
{
    public UpsertEmailSettingsValidator()
    {
        RuleFor(x => x.SmtpHost)
            .MustBeParseable(Hostname.TryFrom);

        RuleFor(x => x.SmtpPort)
            .MustBeParseable(Port.TryFrom);

        RuleFor(x => x.FromAddress)
            .MustBeParseable(EmailAddress.TryFrom);

        RuleFor(x => x.AuthMode)
            .IsInEnum();

        When(x => x.AuthMode == EmailAuthMode.Basic, () =>
        {
            RuleFor(x => x.Username)
                .MustBeParseable(SmtpUsername.TryFrom!);
        });
    }
}
