using ERP.Domain.Common;
using ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Persistence;

public sealed class ErpDbContext(DbContextOptions<ErpDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<CariAccount> CariAccounts => Set<CariAccount>();
    public DbSet<CariDebtItem> CariDebtItems => Set<CariDebtItem>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderItem> SalesOrderItems => Set<SalesOrderItem>();
    public DbSet<FinanceMovement> FinanceMovements => Set<FinanceMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(builder =>
        {
            builder.ToTable("Users");
            builder.HasIndex(x => x.UserName).IsUnique().HasFilter("[IsDeleted] = 0");
            builder.HasIndex(x => x.Email).IsUnique().HasFilter("[IsDeleted] = 0");
            builder.Property(x => x.UserName).HasMaxLength(50).IsRequired();
            builder.Property(x => x.Email).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Role).HasMaxLength(30).IsRequired();

            ConfigureSoftDelete(builder);
        });

        modelBuilder.Entity<CariAccount>(builder =>
        {
            builder.ToTable("CariAccounts");
            builder.HasIndex(x => x.Code).IsUnique().HasFilter("[IsDeleted] = 0");
            builder.Property(x => x.Code).HasMaxLength(25).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
            builder.Property(x => x.RiskLimit).HasPrecision(18, 2);
            builder.Property(x => x.CurrentBalance).HasPrecision(18, 2);
            builder.HasMany(x => x.DebtItems)
                .WithOne(x => x.CariAccount)
                .HasForeignKey(x => x.CariAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            ConfigureSoftDelete(builder);
        });

        modelBuilder.Entity<CariDebtItem>(builder =>
        {
            builder.ToTable("CariDebtItems");
            builder.Property(x => x.MaterialDescription).HasMaxLength(250).IsRequired();
            builder.Property(x => x.Quantity).HasPrecision(18, 3);
            builder.Property(x => x.ListPrice).HasPrecision(18, 2);
            builder.Property(x => x.SalePrice).HasPrecision(18, 2);
            builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
            builder.Property(x => x.Payment).HasPrecision(18, 2);
            builder.Property(x => x.RemainingBalance).HasPrecision(18, 2);
            builder.HasIndex(x => new { x.CariAccountId, x.TransactionDate });

            ConfigureSoftDelete(builder);
        });

        modelBuilder.Entity<Company>(builder =>
        {
            builder.ToTable("Companies");
            builder.HasIndex(x => x.Code).IsUnique().HasFilter("[IsDeleted] = 0");
            builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
            builder.Property(x => x.TaxNumber).HasMaxLength(20).IsRequired();

            ConfigureSoftDelete(builder);
        });

        modelBuilder.Entity<Branch>(builder =>
        {
            builder.ToTable("Branches");
            builder.HasIndex(x => x.Code).IsUnique().HasFilter("[IsDeleted] = 0");
            builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
            builder.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            ConfigureSoftDelete(builder);
        });

        modelBuilder.Entity<Warehouse>(builder =>
        {
            builder.ToTable("Warehouses");
            builder.HasIndex(x => x.Code).IsUnique().HasFilter("[IsDeleted] = 0");
            builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
            builder.HasOne<Branch>()
                .WithMany()
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            ConfigureSoftDelete(builder);
        });

        modelBuilder.Entity<Product>(builder =>
        {
            builder.ToTable("Products");
            builder.HasIndex(x => x.Code).IsUnique().HasFilter("[IsDeleted] = 0");
            builder.HasIndex(x => x.BarcodeEan13).IsUnique().HasFilter("[BarcodeEan13] IS NOT NULL AND [IsDeleted] = 0");
            builder.HasIndex(x => x.QrCode).IsUnique().HasFilter("[QrCode] IS NOT NULL AND [IsDeleted] = 0");
            builder.Property(x => x.Code).HasMaxLength(30).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Unit).HasMaxLength(10).IsRequired();
            builder.Property(x => x.Category).HasMaxLength(100);
            builder.Property(x => x.BarcodeEan13).HasMaxLength(13);
            builder.Property(x => x.QrCode).HasMaxLength(300);
            builder.Property(x => x.DefaultSalePrice).HasPrecision(18, 2);
            builder.Property(x => x.CriticalStockLevel).HasPrecision(18, 3);

            ConfigureSoftDelete(builder);
        });

        modelBuilder.Entity<StockMovement>(builder =>
        {
            builder.ToTable("StockMovements");
            builder.Property(x => x.Quantity).HasPrecision(18, 3);
            builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
            builder.Property(x => x.ReferenceNo).HasMaxLength(50);
            builder.HasOne<Warehouse>()
                .WithMany()
                .HasForeignKey(x => x.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Product>()
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            ConfigureSoftDelete(builder);
        });

        modelBuilder.Entity<PurchaseOrder>(builder =>
        {
            builder.ToTable("PurchaseOrders");
            builder.HasIndex(x => x.OrderNo).IsUnique().HasFilter("[IsDeleted] = 0");
            builder.Property(x => x.OrderNo).HasMaxLength(30).IsRequired();
            builder.HasOne<CariAccount>()
                .WithMany()
                .HasForeignKey(x => x.SupplierCariAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Warehouse>()
                .WithMany()
                .HasForeignKey(x => x.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasMany(x => x.Items)
                .WithOne()
                .HasForeignKey(x => x.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            ConfigureSoftDelete(builder);
        });

        modelBuilder.Entity<PurchaseOrderItem>(builder =>
        {
            builder.ToTable("PurchaseOrderItems");
            builder.Property(x => x.Quantity).HasPrecision(18, 3);
            builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
            builder.HasOne<Product>()
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            ConfigureSoftDelete(builder);
        });

        modelBuilder.Entity<SalesOrder>(builder =>
        {
            builder.ToTable("SalesOrders");
            builder.HasIndex(x => x.OrderNo).IsUnique().HasFilter("[IsDeleted] = 0");
            builder.Property(x => x.OrderNo).HasMaxLength(30).IsRequired();
            builder.HasOne<CariAccount>()
                .WithMany()
                .HasForeignKey(x => x.CustomerCariAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Warehouse>()
                .WithMany()
                .HasForeignKey(x => x.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasMany(x => x.Items)
                .WithOne()
                .HasForeignKey(x => x.SalesOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            ConfigureSoftDelete(builder);
        });

        modelBuilder.Entity<SalesOrderItem>(builder =>
        {
            builder.ToTable("SalesOrderItems");
            builder.Property(x => x.Quantity).HasPrecision(18, 3);
            builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
            builder.HasOne<Product>()
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            ConfigureSoftDelete(builder);
        });

        modelBuilder.Entity<FinanceMovement>(builder =>
        {
            builder.ToTable("FinanceMovements");
            builder.Property(x => x.Amount).HasPrecision(18, 2);
            builder.Property(x => x.Description).HasMaxLength(250);
            builder.Property(x => x.ReferenceNo).HasMaxLength(50);
            builder.HasOne<CariAccount>()
                .WithMany()
                .HasForeignKey(x => x.CariAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            ConfigureSoftDelete(builder);
        });
    }

    private static void ConfigureSoftDelete<TEntity>(EntityTypeBuilder<TEntity> builder)
        where TEntity : BaseEntity
    {
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}


