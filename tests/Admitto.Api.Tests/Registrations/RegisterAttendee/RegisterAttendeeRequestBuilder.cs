// using Amolenk.Admitto.Module.Registrations.Application.UseCases.RegisterAttendee.Admin;
//
// namespace Amolenk.Admitto.Testing.Builders;
//
// public class RegisterAttendeeRequestBuilder
// {
//     private string _email = "test@example.com";
//     private string _firstName = "John";
//     private string _lastName = "Doe";
//     private Dictionary<string, string> _details = new Dictionary<string, string>
//         {
//             { "Company", "Test Company" }
//         };
//
//     private Guid[] _tickets =
//         [];
//
//     public RegisterAttendeeRequestBuilder WithEmail(string email)
//     {
//         _email = email;
//         return this;
//     }
//
//     public RegisterAttendeeRequestBuilder WithFirstName(string firstName)
//     {
//         _firstName = firstName;
//         return this;
//     }
//
//     public RegisterAttendeeRequestBuilder WithLastName(string lastName)
//     {
//         _lastName = lastName;
//         return this;
//     }
//
//     public RegisterAttendeeRequestBuilder WithDetails(Dictionary<string, string> details)
//     {
//         _details = details;
//         return this;
//     }
//
//     public RegisterAttendeeRequestBuilder WithTicketRequests(Guid[] tickets)
//     {
//         _tickets = tickets;
//         return this;
//     }
//
//     public RegisterAttendeeHttpRequest Build()
//     {
//         return new RegisterAttendeeHttpRequest(_firstName, _lastName, _email, _tickets);// _firstName, _lastName, _details, _tickets!);
//     }
// }