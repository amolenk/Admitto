// using FluentValidation;
//
// namespace Amolenk.Admitto.Application.Features.TicketedEvents.CreateTicketedEvent;
//
// public class CreateTicketedEventValidator : AbstractValidator<CreateTicketedEventCommand>
// {
//     public CreateTicketedEventValidator()
//     {
//         RuleFor(x => x.Name)
//             .NotEmpty().WithMessage("Event name is required.")
//             .MaximumLength(100).WithMessage("Event name must be less than 100 characters.");
//
//         RuleFor(x => x.Date)
//             .NotEmpty().WithMessage("Event date is required.");
//
//         RuleFor(x => x.Location)
//             .NotEmpty().WithMessage("Event location is required.");
//     }
// }