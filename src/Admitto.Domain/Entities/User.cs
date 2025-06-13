// using Amolenk.Admitto.Domain.ValueObjects;
//
// namespace Amolenk.Admitto.Domain.Entities;
//
// public enum UserRole
// {
//     GlobalAdmin = 0,
//     TeamManager = 1,
//     TeamOrganizer = 2,
//     EventContributor = 3
// }
//
// /// <summary>
// /// Represents a user in the system.
// /// </summary>
// public class User : Entity
// {
//     // EF Core constructor
//     private User() { }
//     
//     private User(UserId id, string email) : base(id)
//     {
//         Id = id.Value;
//         Email = email;
//     }
//     
//     public string Email { get; private set; } = null!;
//     public UserRole Role { get; private set; }
//
//     public static User Create(string email)
//     {
//         var id = UserId.FromEmail(email);
//         
//         return new User(id, email);
//     }
// }
