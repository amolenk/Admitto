using Amolenk.Admitto.Domain;
using FluentValidation.Results;

namespace Amolenk.Admitto.Application.Common.Validation;

public static class ValidationErrorCode
{
    public static class EventSlug
    {
        public const string Required = "TicketedEvent.AlreadyExists";
        public const string NotFound = "TicketedEvent.NotFound";
        public const string SoldOut = "TicketedEvent.SoldOut";
    }
}

public static class ValidationError
{
    public static class AttendeeRegistration
    {
        public static ValidationException AlreadyExists()
        {
            return new ValidationException(ErrorMessage.AttendeeRegistration.AlreadyExists);
        }

        public static ValidationException NotFound(Guid id)
        {
            return new ValidationException(ErrorMessage.AttendeeRegistration.NotFound(id));
        }
    }

    public static class Team
    {
        public static ValidationException NotFound(string teamSlug)
        {
            return new ValidationException(ErrorMessage.Team.NotFound(teamSlug));
        }
    }
    
    public static class TicketedEvent
    {
        public static ValidationException NotFound(Guid teamId)
        {
            return new ValidationException(ErrorMessage.TicketedEvent.NotFound(teamId));
        }

        public static ValidationException AlreadyExists(string ticketedEventNamePropertyName)
        {
            return new ValidationException(ErrorMessage.TicketedEvent.AlreadyExists, [
                new ValidationFailure(
                    ticketedEventNamePropertyName, ErrorMessage.TicketedEvent.Name.MustBeUnique)]);
        }

        // TODO Include the current capacity in the error details (for each ticket type).
        public static ValidationException SoldOut()
        {
            return new ValidationException(ErrorMessage.TicketedEvent.SoldOut);
        }
    }
}