using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Amolenk.Admitto.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Order> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultContainer("Orders");

        modelBuilder.Entity<Order>()
            .ToContainer("Orders")
            .HasPartitionKey(o => o.PartitionKey)
            .HasNoDiscriminator()
            .UseETagConcurrency();
        
        modelBuilder.Entity<Order>().OwnsOne(
            o => o.ShippingAddress,
            sa =>
            {
                sa.ToJsonProperty("Address");
                sa.Property(p => p.Street).ToJsonProperty("ShipsToStreet");
                sa.Property(p => p.City).ToJsonProperty("ShipsToCity");
            });
    }
}

public class Order
{
    public int Id { get; set; }
    public int? TrackingNumber { get; set; }
    
    [JsonProperty("partitionKey")]
    public string PartitionKey { get; set; } = "default";
    public StreetAddress ShippingAddress { get; set; }
}

public class StreetAddress
{
    public string Street { get; set; }
    public string City { get; set; }
}