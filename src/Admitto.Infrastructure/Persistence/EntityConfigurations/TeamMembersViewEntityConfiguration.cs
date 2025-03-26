// using Amolenk.Admitto.Application.ReadModel.Views;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.EntityFrameworkCore.Metadata.Builders;
//
// namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;
//
// public class TeamMembersViewEntityConfiguration : IEntityTypeConfiguration<TeamMembersView>
// {
//     public void Configure(EntityTypeBuilder<TeamMembersView> builder)
//     {
//         builder.ToTable("team_members");
//         builder.HasKey(e => e.Id);
//         
//         builder.Property(e => e.Id)
//             .HasColumnName("id")
//             .ValueGeneratedNever();
//
//         builder.Property(e => e.Name)
//             .HasColumnName("name")
//             .IsRequired()
//             .HasMaxLength(50);
//         
//         // builder.Property(e => e.Members)
//         //     .HasColumnName("members")
//         //     .HasColumnType("jsonb")
//         //     .IsRequired();
//         
//         builder.OwnsMany(e => e.Members, b =>
//         {
//             b.ToJson("members");
//         });
//     }
// }
