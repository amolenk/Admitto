// using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
// using Amolenk.Admitto.Module.Shared.Application.Validation;
// using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
// using FluentValidation;
//
// namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.RegisterAttendee.Public;
//
// public class SelfRegisterAttendeeValidator : AbstractValidator<SelfRegisterAttendeeHttpRequest>
// {
//     public SelfRegisterAttendeeValidator()
//     {
//         RuleFor(x => x.Email)
//             .MustBeParseable(EmailAddress.TryFrom);
//
//         RuleForEach(x => x.TicketTypeSlugs)
//             .MustBeParseable(TicketTypeId.TryFrom);
//     }
// }
