using Microsoft.EntityFrameworkCore;


class Store(DbContextOptions<Store> options) : DbContext(options)
{
    public DbSet<Account> Users => Set<Account>();
    public DbSet<StudentDocument> Documents => Set<StudentDocument>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<AccountRole> UserRoles => Set<AccountRole>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>().ToTable("Users");
        modelBuilder.Entity<Account>().HasIndex(user => user.Email).IsUnique();
        modelBuilder.Entity<AccountRole>().ToTable("UserRoles").HasKey(role => new { role.UserId, role.Role });
        modelBuilder.Entity<AccountRole>().Property(role => role.Role).HasConversion<string>();
        modelBuilder.Entity<Account>().HasMany(user => user.Roles).WithOne()
            .HasForeignKey(role => role.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Order>().Property(order => order.PreparationStatus).HasConversion<string>();
        modelBuilder.Entity<Order>().Property(order => order.PaymentStatus).HasConversion<string>();
        modelBuilder.Entity<Order>().Property(order => order.Version).IsConcurrencyToken();
    }
}
