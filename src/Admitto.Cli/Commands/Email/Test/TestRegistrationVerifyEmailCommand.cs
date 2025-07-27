// using Microsoft.Extensions.Configuration;
//
// namespace Amolenk.Admitto.Cli.Commands.Email.Test;
//
// public class TestRegistrationEmailSettings : TestEmailSettings
// {
//     [CommandOption("--registrationId")] 
//     public required Guid RegistrationId { get; init; }
//
//     public override ValidationResult Validate()
//     {
//         if (RegistrationId == Guid.Empty)
//         {
//             return ValidationResult.Error("Registration ID must be specified and cannot be empty.");
//         }
//         
//         return base.Validate();
//     }
// }
//
// public class TestRegistrationVerifyEmailCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
//     : TestEmailCommand<TestRegistrationEmailSettings>(EmailType.VerifyRegistration, accessTokenProvider, configuration)
// {
//     protected override Guid GetDataEntityId(TestRegistrationEmailSettings settings) => settings.RegistrationId;
// }
