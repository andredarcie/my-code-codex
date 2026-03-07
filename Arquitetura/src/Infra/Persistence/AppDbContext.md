# AppDbContext

DbContext do Entity Framework Core. Configura o mapeamento das entidades e ignora os Domain Events (não são persistidos).

```csharp
namespace Infra.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(builder =>
        {
            builder.HasKey(o => o.Id);
            builder.Property(o => o.CustomerId).IsRequired();
            builder.Property(o => o.Total).HasPrecision(18, 2);
            builder.Property(o => o.Status).HasConversion<string>();

            builder.HasMany(o => o.Items)
                   .WithOne()
                   .HasForeignKey("OrderId")
                   .OnDelete(DeleteBehavior.Cascade);

            builder.Ignore(o => o.DomainEvents);
        });

        modelBuilder.Entity<OrderItem>(builder =>
        {
            builder.HasKey(i => i.Id);
            builder.Property(i => i.Price).HasPrecision(18, 2);
        });
    }
}
```
