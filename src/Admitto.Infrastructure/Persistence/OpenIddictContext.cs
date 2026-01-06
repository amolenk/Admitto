// using Microsoft.EntityFrameworkCore;
//
// namespace Amolenk.Admitto.Infrastructure.Persistence;
//
// public class OpenIddictContext(DbContextOptions<OpenIddictContext> options) : DbContext(options)
// {
//     protected override void OnModelCreating(ModelBuilder builder)
//     {
//         base.OnModelCreating(builder);
//
//         // Registers the default OpenIddict entities (applications, authorizations, scopes, tokens)
//         builder.UseOpenIddict();
//     }
// }