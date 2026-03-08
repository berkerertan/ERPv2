using ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Persistence;

public sealed class ErpDbContext(DbContextOptions<ErpDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<CariAccount> CariAccounts => Set<CariAccount>();
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
            builder.HasIndex(x => x.UserName).IsUnique();
            builder.HasIndex(x => x.Email).IsUnique();
            builder.Property(x => x.UserName).HasMaxLength(50).IsRequired();
            builder.Property(x => x.Email).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Role).HasMaxLength(30).IsRequired();
        });

        modelBuilder.Entity<CariAccount>(builder =>
        {
            builder.ToTable("CariAccounts");
            builder.HasIndex(x => x.Code).IsUnique();
            builder.Property(x => x.Code).HasMaxLength(25).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
            builder.Property(x => x.RiskLimit).HasPrecision(18, 2);
            builder.Property(x => x.CurrentBalance).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Company>(builder =>
        {
            builder.ToTable("Companies");
            builder.HasIndex(x => x.Code).IsUnique();
            builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
            builder.Property(x => x.TaxNumber).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<Branch>(builder =>
        {
            builder.ToTable("Branches");
            builder.HasIndex(x => x.Code).IsUnique();
            builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
            builder.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Warehouse>(builder =>
        {
            builder.ToTable("Warehouses");
            builder.HasIndex(x => x.Code).IsUnique();
            builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
            builder.HasOne<Branch>()
                .WithMany()
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Product>(builder =>
        {
            builder.ToTable("Products");
            builder.HasIndex(x => x.Code).IsUnique();
            builder.Property(x => x.Code).HasMaxLength(30).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Unit).HasMaxLength(10).IsRequired();
            builder.Property(x => x.Category).HasMaxLength(100);
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
        });

        modelBuilder.Entity<PurchaseOrder>(builder =>
        {
            builder.ToTable("PurchaseOrders");
            builder.HasIndex(x => x.OrderNo).IsUnique();
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
        });

        modelBuilder.Entity<SalesOrder>(builder =>
        {
            builder.ToTable("SalesOrders");
            builder.HasIndex(x => x.OrderNo).IsUnique();
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
        });
    }
}
