namespace Amolenk.Admitto.Domain;

public static class ErrorMessage
{
    [Obsolete]
    public const string Conflict = "Conflict";

    [Obsolete]
    public const string ValidationFailed = "Validation failed";

    public static class Attendee
    {
        public static class Email
        {
            public const string IsRequired = "Attendee email is required.";
            public const string MustBeValid = "Attendee email must be a valid email address.";
        }

        public static class FirstName
        {
            public const string IsRequired = "Attendee first name is required.";
            public const string MustBeMin2Length = "Attendee first name must be at least 2 characters long.";
            public const string MustBeMax50Length = "Attendee first name must be 50 characters or less.";
        }
        
        public static class LastName
        {
            public const string IsRequired = "Attendee last name is required.";
            public const string MustBeMin2Length = "Attendee last name must be at least 2 characters long.";
            public const string MustBeMax50Length = "Attendee last name must be 50 characters or less.";
        }
        
        public static class Details
        {
            public const string AreRequired = "Attendee details are required.";

            public static class Key
            {
                public const string IsRequired = "Attendee detail key is required.";
                public const string MustNotBeEmpty = "Attendee detail key must not be empty.";
                public const string MustBeMax50Length = "Attendee detail key must be 50 characters or less.";
            }
            
            public static class Value
            {
                public const string IsRequired = "Attendee detail value is required.";
                public const string MustBeMax50Length = "Attendee detail value must be 50 characters or less.";
                
            }
        }
    }

    public static class AttendeeRegistration
    {
        public static string NotFound(Guid registrationId) => $"Attendee registration {registrationId} not found.";
        public const string AlreadyExists = "Attendee is already registered.";

        public static class Tickets
        {
            public const string AreRequired = "Tickets are required.";
            public const string MustNotBeEmpty = "Tickets must not be empty.";

            public static class TicketType
            {
                public const string MustNotBeEmpty = "Ticket type must not be empty.";
                public const string AlreadyExists = "Ticket type already exists in the registration.";
            }

            public static class Quantity
            {
                public const string MustNotBeEmpty = "Ticket quantity must not be empty.";
                public const string MustBeGreaterThanZero = "Ticket quantity must be greater than zero.";
            }
        }
    }

    public static class Team
    {
        public static string NotFound(string teamSlug) => $"Team {teamSlug} not found.";
        public const string AlreadyExists = "A team with this name already exists.";

        public static class Name
        {
            public const string MustBeUnique = "Team name must be unique.";
        }
    }

    public static class Ticket
    {
        public static class Quantity
        {
            public const string MustBeGreaterThanZero = "Ticket quantity must be greater than zero.";
        }
    }

    public static class TicketedEvent
    {
        public static string NotFound(Guid ticketedEventId) => $"Event {ticketedEventId} not found.";

        
//        public const string MustExist = "Event must exist.";
        public const string AlreadyExists = "An event with this name already exists.";
        public const string SoldOut = "Some or all of the tickets are sold out.";

        public class Id
        {
            public const string MustNotBeEmpty = "Event ID must not be empty.";
        }
        
        public static class Name
        {
            public const string MustBeUnique = "Event name must be unique.";
        }
        
        public static class TicketType
        {
            public const string AlreadyExists = "Ticket type already exists for event.";
        }
    }
}
