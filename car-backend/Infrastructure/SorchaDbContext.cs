using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public class SorchaDbContext : DbContext
{
    public SorchaDbContext(DbContextOptions<SorchaDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<CarModel> CarModels => Set<CarModel>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Car> Cars => Set<Car>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Part> Parts => Set<Part>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SorchaDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(EntityBase).IsAssignableFrom(entityType.ClrType) && !entityType.IsAbstract())
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(EntityBase.Id))
                    .HasDefaultValueSql("gen_random_uuid()");

                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(EntityBase.CreatedAt))
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(EntityBase.UpdatedAt))
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            }
        }

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Role).HasMaxLength(20);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(r => r.Token).IsUnique();
            entity.Property(r => r.Token).HasMaxLength(64);
            entity.HasIndex(r => r.UserId);
        });

        modelBuilder.Entity<Brand>(entity =>
        {
            entity.HasIndex(b => b.Name).IsUnique();
        });

        modelBuilder.Entity<CarModel>(entity =>
        {
            entity.HasIndex(m => m.Name).IsUnique();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(c => c.Name).IsUnique();
            entity.HasIndex(c => c.Slug).IsUnique();
            entity.HasIndex(c => c.Type);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.Property(p => p.ProductType).HasMaxLength(10);
            entity.Property(p => p.Status).HasMaxLength(20);
            entity.Property(p => p.Price).HasColumnType("decimal(12,2)");
            entity.Property(p => p.Specs).HasColumnType("jsonb");
            entity.HasIndex(p => p.Status).HasDatabaseName("IX_products_status");
            entity.HasIndex(p => p.ProductType);
            entity.HasOne(p => p.Category).WithMany().HasForeignKey(p => p.CategoryId);
            entity.HasOne(p => p.Seller).WithMany().HasForeignKey(p => p.SellerId);
            entity.HasMany(p => p.ProductImages)
                .WithOne(pi => pi.Product)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Car>(entity =>
        {
            entity.ToTable("Cars");
            entity.HasIndex(c => c.Year);
            entity.HasOne(c => c.Brand).WithMany().HasForeignKey(c => c.BrandId);
            entity.HasOne(c => c.Model).WithMany().HasForeignKey(c => c.ModelId);
        });

        modelBuilder.Entity<Part>(entity =>
        {
            entity.ToTable("Parts");
            entity.Property(p => p.Name).HasMaxLength(200);
            entity.Property(p => p.Brand).HasMaxLength(100);
            entity.Property(p => p.Sku).HasMaxLength(100);
            entity.HasIndex(p => p.Sku).IsUnique();
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasIndex(pi => pi.ProductId);
        });

        modelBuilder.Entity<Favorite>(entity =>
        {
            entity.HasKey(f => new { f.UserId, f.ProductId });
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(c => new { c.UserId, c.ProductId });
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.Property(oi => oi.UnitPrice).HasColumnType("decimal(12,2)");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(o => o.TotalAmount).HasColumnType("decimal(12,2)");
        });
    }
}
