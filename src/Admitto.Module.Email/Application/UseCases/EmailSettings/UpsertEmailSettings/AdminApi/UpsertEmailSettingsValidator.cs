using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Validation;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailSettings.UpsertEmailSettings.AdminApi;

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
