using ERP.Application.Abstractions.Security;
using ERP.Domain.Common;
using ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Persistence;

public sealed class ErpDbContext(DbContextOptions<ErpDbContext> options, ICurrentTenantService currentTenantService) : DbContext(options)
{
    private Guid CurrentTenantIdOrEmpty => currentTenantService.TenantId ?? Guid.Empty;
    private bool BypassTenantFilter => currentTenantService.IsPlatformAdmin;

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<TenantAccount> TenantAccounts => Set<TenantAccount>();
    public DbSet<SubscriptionPlanSetting> SubscriptionPlanSettings => Set<SubscriptionPlanSetting>();
    public DbSet<LandingPageContent> LandingPageContents => Set<LandingPageContent>();
    public DbSet<SystemActivityLog> SystemActivityLogs => Set<SystemActivityLog>();

    public DbSet<ChartOfAccount> ChartOfAccounts => Set<ChartOfAccount>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();
    public DbSet<CashAccount> CashAccounts => Set<CashAccount>();
    public DbSet<CashTransaction> CashTransactions => Set<CashTransaction>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<BankTransaction> BankTransactions => Set<BankTransaction>();

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
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(builder =>
        {
            builder.ToTable("Users");
            builder.HasIndex(x => x.UserName).IsUnique().HasFilter("[IsDeleted] = 0");
            builder.HasIndex(x => x.Email).IsUnique().HasFilter("[IsDeleted] = 0");
            builder.HasIndex(x => x.TenantAccountId);
            builder.Property(x => x.UserName).HasMaxLength(50).IsRequired();
            builder.Property(x => x.Email).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Role).HasMaxLength(30).IsRequired();
            builder.HasOne<TenantAccount>()
                .WithMany()
                .HasForeignKey(x => x.TenantAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            ConfigureSoftDelete(builder);
        });

        modelBuilder.Entity<TenantAccount>(builder =>
        {
            builder.ToTable("TenantAccounts");
            builder.HasIndex(x => x.Code).IsUnique().HasFilter("[IsDeleted] = 0");
            builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
            builder.Property(x => x.Code).HasMaxLength(40).IsRequired();

            ConfigureSoftDelete(builder);
        });

        modelBuilder.Entity<SubscriptionPlanSetting>(builder =>
        {
            builder.ToTable("SubscriptionPlanSettings");
            builder.HasIndex(x => x.Plan).IsUnique().HasFilter("[IsDeleted] = 0");
            builder.Property(x => x.DisplayName).HasMaxLength(80).IsRequired();
            builder.Property(x => x.MonthlyPrice).HasPrecision(18, 2);
            builder.Property(x => x.FeaturesCsv).HasMaxLength(1000).IsRequired();

            ConfigureSoftDelete(builder);
        });

        modelBuilder.Entity<LandingPageContent>(builder =>
        {
            builder.ToTable("LandingPageContents");
            builder.HasIndex(x => x.Key).IsUnique().HasFilter("[IsDeleted] = 0");
            builder.Property(x => x.Key).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Content).HasMaxLength(4000).IsRequired();

            ConfigureSoftDelete(builder);
        });

        modelBuilder.Entity<SystemActivityLog>(builder =>
        {
            builder.ToTable("SystemActivityLogs");
            builder.HasIndex(x => x.OccurredAtUtc);
            builder.HasIndex(x => x.TenantAccountId);
            builder.HasIndex(x => x.UserId);
            builder.Property(x => x.UserName).HasMaxLength(100);
            builder.Property(x => x.HttpMethod).HasMaxLength(10).IsRequired();
            builder.Property(x => x.Path).HasMaxLength(500).IsRequired();
            builder.Property(x => x.IpAddress).HasMaxLength(100);
            builder.Property(x => x.UserAgent).HasMaxLength(1000);
            builder.HasOne<TenantAccount>()
                .WithMany()
                .HasForeignKey(x => x.TenantAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            ConfigureSoftDelete(builder);
        });

        modelBuilder.Entity<ChartOfAccount>(builder =>
        {
            builder.ToTable("ChartOfAccounts");
            builder.HasIndex(x => new { x.TenantAccountId, x.Code }).IsUnique().HasFilter("[IsDeleted] = 0");
            builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(200).IsRequired();

            ConfigureTenantEntity(builder);
        });

        modelBuilder.Entity<JournalEntry>(builder =>
        {
            builder.ToTable("JournalEntries");
            builder.HasIndex(x => new { x.TenantAccountId, x.VoucherNo }).IsUnique().HasFilter("[IsDeleted] = 0");
            builder.Property(x => x.VoucherNo).HasMaxLength(30).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(500);
            builder.HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(x => x.JournalEntryId)
                .OnDelete(DeleteBehavior.Cascade);

            ConfigureTenantEntity(builder);
        });

        modelBuilder.Entity<JournalEntryLine>(builder =>
        {
            builder.ToTable("JournalEntryLines");
            builder.Property(x => x.Debit).HasPrecision(18, 2);
            builder.Property(x => x.Credit).HasPrecision(18, 2);
            builder.Property(x => x.Description).HasMaxLength(300);
            builder.HasOne<ChartOfAccount>()
                .WithMany()
                .HasForeignKey(x => x.ChartOfAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            ConfigureTenantEntity(builder);
        });

        modelBuilder.Entity<CashAccount>(builder =>
        {
            builder.ToTable("CashAccounts");
            builder.HasIndex(x => new { x.TenantAccountId, x.Code }).IsUnique().HasFilter("[IsDeleted] = 0");
            builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
            builder.Property(x => x.Currency).HasMaxLength(5).IsRequired();
            builder.Property(x => x.Balance).HasPrecision(18, 2);

            ConfigureTenantEntity(builder);
        });

        modelBuilder.Entity<CashTransaction>(builder =>
        {
            builder.ToTable("CashTransactions");
            builder.HasIndex(x => new { x.TenantAccountId, x.CashAccountId, x.TransactionDateUtc });
            builder.HasIndex(x => x.FinanceMovementId);
            builder.Property(x => x.Amount).HasPrecision(18, 2);
            builder.Property(x => x.Description).HasMaxLength(300);
            builder.Property(x => x.ReferenceNo).HasMaxLength(50);
            builder.HasOne<CashAccount>()
                .WithMany()
                .HasForeignKey(x => x.CashAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<CariAccount>()
                .WithMany()
                .HasForeignKey(x => x.CariAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<FinanceMovement>()
                .WithMany()
                .HasForeignKey(x => x.FinanceMovementId)
                .OnDelete(DeleteBehavior.SetNull);

            ConfigureTenantEntity(builder);
        });

        modelBuilder.Entity<BankAccount>(builder =>
        {
            builder.ToTable("BankAccounts");
            builder.HasIndex(x => new { x.TenantAccountId, x.Iban }).IsUnique().HasFilter("[IsDeleted] = 0");
            builder.Property(x => x.BankName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.BranchName).HasMaxLength(100);
            builder.Property(x => x.Iban).HasMaxLength(34).IsRequired();
            builder.Property(x => x.Currency).HasMaxLength(5).IsRequired();
            builder.Property(x => x.Balance).HasPrecision(18, 2);

            ConfigureTenantEntity(builder);
        });

        modelBuilder.Entity<BankTransaction>(builder =>
        {
            builder.ToTable("BankTransactions");
            builder.HasIndex(x => new { x.TenantAccountId, x.BankAccountId, x.TransactionDateUtc });
            builder.HasIndex(x => x.FinanceMovementId);
            builder.Property(x => x.Amount).HasPrecision(18, 2);
            builder.Property(x => x.Description).HasMaxLength(300);
            builder.Property(x => x.ReferenceNo).HasMaxLength(50);
            builder.HasOne<BankAccount>()
                .WithMany()
                .HasForeignKey(x => x.BankAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<CariAccount>()
                .WithMany()
                .HasForeignKey(x => x.CariAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<FinanceMovement>()
                .WithMany()
                .HasForeignKey(x => x.FinanceMovementId)
                .OnDelete(DeleteBehavior.SetNull);

            ConfigureTenantEntity(builder);
        });

        modelBuilder.Entity<CariAccount>(builder =>
        {
            builder.ToTable("CariAccounts");
            builder.HasIndex(x => new { x.TenantAccountId, x.Code }).IsUnique().HasFilter("[IsDeleted] = 0");
            builder.Property(x => x.Code).HasMaxLength(25).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
            builder.Property(x => x.RiskLimit).HasPrecision(18, 2);
            builder.Property(x => x.CurrentBalance).HasPrecision(18, 2);
            builder.HasMany(x => x.DebtItems)
                .WithOne(x => x.CariAccount)
                .HasForeignKey(x => x.CariAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            ConfigureTenantEntity(builder);
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
            builder.HasIndex(x => new { x.TenantAccountId, x.CariAccountId, x.TransactionDate });

            ConfigureTenantEntity(builder);
        });

        modelBuilder.Entity<Company>(builder =>
        {
            builder.ToTable("Companies");
            builder.HasIndex(x => new { x.TenantAccountId, x.Code }).IsUnique().HasFilter("[IsDeleted] = 0");
            builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
            builder.Property(x => x.TaxNumber).HasMaxLength(20).IsRequired();

            ConfigureTenantEntity(builder);
        });

        modelBuilder.Entity<Branch>(builder =>
        {
            builder.ToTable("Branches");
            builder.HasIndex(x => new { x.TenantAccountId, x.Code }).IsUnique().HasFilter("[IsDeleted] = 0");
            builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
            builder.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            ConfigureTenantEntity(builder);
        });

        modelBuilder.Entity<Warehouse>(builder =>
        {
            builder.ToTable("Warehouses");
            builder.HasIndex(x => new { x.TenantAccountId, x.Code }).IsUnique().HasFilter("[IsDeleted] = 0");
            builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
            builder.HasOne<Branch>()
                .WithMany()
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            ConfigureTenantEntity(builder);
        });

        modelBuilder.Entity<Product>(builder =>
        {
            builder.ToTable("Products");
            builder.HasIndex(x => new { x.TenantAccountId, x.Code }).IsUnique().HasFilter("[IsDeleted] = 0");
            builder.HasIndex(x => new { x.TenantAccountId, x.BarcodeEan13 }).IsUnique().HasFilter("[BarcodeEan13] IS NOT NULL AND [IsDeleted] = 0");
            builder.HasIndex(x => new { x.TenantAccountId, x.QrCode }).IsUnique().HasFilter("[QrCode] IS NOT NULL AND [IsDeleted] = 0");
            builder.Property(x => x.Code).HasMaxLength(30).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Unit).HasMaxLength(10).IsRequired();
            builder.Property(x => x.Category).HasMaxLength(100);
            builder.Property(x => x.BarcodeEan13).HasMaxLength(13);
            builder.Property(x => x.QrCode).HasMaxLength(300);
            builder.Property(x => x.DefaultSalePrice).HasPrecision(18, 2);
            builder.Property(x => x.CriticalStockLevel).HasPrecision(18, 3);

            ConfigureTenantEntity(builder);
        });

        modelBuilder.Entity<StockMovement>(builder =>
        {
            builder.ToTable("StockMovements");
            builder.HasIndex(x => new { x.TenantAccountId, x.WarehouseId, x.ProductId, x.MovementDateUtc });
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

            ConfigureTenantEntity(builder);
        });

        modelBuilder.Entity<PurchaseOrder>(builder =>
        {
            builder.ToTable("PurchaseOrders");
            builder.HasIndex(x => new { x.TenantAccountId, x.OrderNo }).IsUnique().HasFilter("[IsDeleted] = 0");
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

            ConfigureTenantEntity(builder);
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

            ConfigureTenantEntity(builder);
        });

        modelBuilder.Entity<SalesOrder>(builder =>
        {
            builder.ToTable("SalesOrders");
            builder.HasIndex(x => new { x.TenantAccountId, x.OrderNo }).IsUnique().HasFilter("[IsDeleted] = 0");
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

            ConfigureTenantEntity(builder);
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

            ConfigureTenantEntity(builder);
        });

        modelBuilder.Entity<Invoice>(builder =>
        {
            builder.ToTable("Invoices");
            builder.HasIndex(x => new { x.TenantAccountId, x.SalesOrderId }).IsUnique().HasFilter("[SalesOrderId] IS NOT NULL AND [IsDeleted] = 0");
            builder.HasIndex(x => new { x.TenantAccountId, x.PurchaseOrderId }).IsUnique().HasFilter("[PurchaseOrderId] IS NOT NULL AND [IsDeleted] = 0");
            builder.Property(x => x.InvoiceNumber).HasMaxLength(40);
            builder.Property(x => x.CariAccountName).HasMaxLength(150).IsRequired();
            builder.Property(x => x.TaxNumber).HasMaxLength(20);
            builder.Property(x => x.Currency).HasMaxLength(5).IsRequired();
            builder.Property(x => x.Subtotal).HasPrecision(18, 2);
            builder.Property(x => x.TaxTotal).HasPrecision(18, 2);
            builder.Property(x => x.DiscountTotal).HasPrecision(18, 2);
            builder.Property(x => x.GrandTotal).HasPrecision(18, 2);
            builder.Property(x => x.Uuid).HasMaxLength(100);
            builder.Property(x => x.Ettn).HasMaxLength(100);
            builder.Property(x => x.GibResponseCode).HasMaxLength(50);
            builder.Property(x => x.GibResponseDescription).HasMaxLength(250);
            builder.Property(x => x.Notes).HasMaxLength(500);
            builder.HasOne<CariAccount>()
                .WithMany()
                .HasForeignKey(x => x.CariAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SalesOrder>()
                .WithMany()
                .HasForeignKey(x => x.SalesOrderId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<PurchaseOrder>()
                .WithMany()
                .HasForeignKey(x => x.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasMany(x => x.Items)
                .WithOne()
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            ConfigureTenantEntity(builder);
        });

        modelBuilder.Entity<InvoiceItem>(builder =>
        {
            builder.ToTable("InvoiceItems");
            builder.Property(x => x.ProductName).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Barcode).HasMaxLength(50);
            builder.Property(x => x.Unit).HasMaxLength(20).IsRequired();
            builder.Property(x => x.Quantity).HasPrecision(18, 3);
            builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
            builder.Property(x => x.DiscountRate).HasPrecision(18, 2);
            builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            builder.Property(x => x.TaxRate).HasPrecision(18, 2);
            builder.Property(x => x.TaxAmount).HasPrecision(18, 2);
            builder.Property(x => x.LineTotal).HasPrecision(18, 2);
            builder.HasOne<Product>()
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            ConfigureTenantEntity(builder);
        });

        modelBuilder.Entity<FinanceMovement>(builder =>
        {
            builder.ToTable("FinanceMovements");
            builder.HasIndex(x => new { x.TenantAccountId, x.CariAccountId, x.MovementDateUtc });
            builder.Property(x => x.Amount).HasPrecision(18, 2);
            builder.Property(x => x.Description).HasMaxLength(250);
            builder.Property(x => x.ReferenceNo).HasMaxLength(50);
            builder.HasOne<CariAccount>()
                .WithMany()
                .HasForeignKey(x => x.CariAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            ConfigureTenantEntity(builder);
        });
    }

    public override int SaveChanges()
    {
        ApplyTenantScope();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyTenantScope();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyTenantScope();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyTenantScope();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyTenantScope()
    {
        foreach (var entry in ChangeTracker.Entries<ITenantOwnedEntity>())
        {
            if (entry.State is EntityState.Detached or EntityState.Unchanged)
            {
                continue;
            }

            if (BypassTenantFilter)
            {
                if (entry.State == EntityState.Added && entry.Entity.TenantAccountId == Guid.Empty && CurrentTenantIdOrEmpty != Guid.Empty)
                {
                    entry.Entity.TenantAccountId = CurrentTenantIdOrEmpty;
                }

                continue;
            }

            if (CurrentTenantIdOrEmpty == Guid.Empty)
            {
                if (entry.Entity.TenantAccountId == Guid.Empty)
                {
                    throw new InvalidOperationException("Tenant context is required for tenant-owned data.");
                }

                continue;
            }

            if (entry.State == EntityState.Added && entry.Entity.TenantAccountId == Guid.Empty)
            {
                entry.Entity.TenantAccountId = CurrentTenantIdOrEmpty;
                continue;
            }

            if (entry.Entity.TenantAccountId != CurrentTenantIdOrEmpty)
            {
                throw new InvalidOperationException("Cross-tenant write attempt blocked.");
            }
        }
    }

    private static void ConfigureSoftDelete<TEntity>(EntityTypeBuilder<TEntity> builder)
        where TEntity : BaseEntity
    {
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }

    private void ConfigureTenantEntity<TEntity>(EntityTypeBuilder<TEntity> builder)
        where TEntity : TenantOwnedEntity
    {
        builder.Property(x => x.TenantAccountId).IsRequired();
        builder.HasIndex(x => x.TenantAccountId);
        builder.HasOne<TenantAccount>()
            .WithMany()
            .HasForeignKey(x => x.TenantAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter(x => !x.IsDeleted && (BypassTenantFilter || x.TenantAccountId == CurrentTenantIdOrEmpty));
    }
}
