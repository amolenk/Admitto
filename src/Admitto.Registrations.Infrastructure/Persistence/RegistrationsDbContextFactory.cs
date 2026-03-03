// using Microsoft.EntityFrameworkCore;
// using Microsoft.EntityFrameworkCore.Design;
//
// namespace Amolenk.Admitto.Registrations.Infrastructure.Persistence;
//
// /// <summary>
// /// Factory for creating <see cref="RegistrationsDbContext"/> instances at design time.
// /// Required for EF Core tools like migrations.
// /// </summary>
// public class RegistrationsDbContextFactory : IDesignTimeDbContextFactory<RegistrationsDbContext>
// {
//     public RegistrationsDbContext CreateDbContext(string[] args)
//     {
//         var optionsBuilder = new DbContextOptionsBuilder<RegistrationsDbContext>();
//         optionsBuilder.UseNpgsql();
//         
//         return new RegistrationsDbContext(optionsBuilder.Options);
//     }
// }