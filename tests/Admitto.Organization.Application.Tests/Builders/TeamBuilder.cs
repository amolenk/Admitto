// using Amolenk.Admitto.Organization.Domain.Entities;
// using Amolenk.Admitto.Organization.Domain.ValueObjects;
// using Amolenk.Admitto.Shared.Kernel.ValueObjects;
//
// namespace Amolenk.Admitto.Organization.Application.Tests.Builders;
//
// public class TeamBuilder
// {
//     private TeamSlug _slug = TeamSlug.From("team-slug");
//     private string _name = "Team Name";
//     private EmailAddress _email = EmailAddress.From("team@example.com");
//
//     public TeamBuilder WithSlug(TeamSlug slug)
//     {
//         _slug = slug;
//         return this;
//     }
//
//     public TeamBuilder WithName(string name)
//     {
//         _name = name;
//         return this;
//     }
//
//     public TeamBuilder WithEmail(EmailAddress email)
//     {
//         _email = email;
//         return this;
//     }
//
//     public Team Build() => Team.Create(_slug, _name, _email);
// }