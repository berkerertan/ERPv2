using ERP.Application.Abstractions.Security;
using ERP.Domain.Constants;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using ERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERP.API.Common;

public static class DevelopmentDataSeeder
{
    private static readonly SemaphoreSlim SeedLock = new(1, 1);

    public static async Task SeedAsync(WebApplication app, CancellationToken cancellationToken = default)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        await SeedLock.WaitAsync(cancellationToken);
        try
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var dbContext = services.GetRequiredService<ErpDbContext>();
            var passwordHasher = services.GetRequiredService<IPasswordHasher>();
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DevelopmentDataSeeder");

            await dbContext.Database.MigrateAsync(cancellationToken);

            await EnsureDefaultPlanSettingsAsync(dbContext, cancellationToken);
            await EnsureDefaultLandingContentAsync(dbContext, cancellationToken);
            await EnsureDefaultEmailTemplatesAsync(dbContext, cancellationToken);

            // Demo abonelik tenant'lari
            var demoTier3Tenant = await EnsureTenantAsync(
                dbContext,
                name: "Demo Enterprise",
                code: "demo-tier3",
                plan: SubscriptionPlan.Enterprise,
                cancellationToken);

            var demoTier2Tenant = await EnsureTenantAsync(
                dbContext,
                name: "Demo Growth",
                code: "demo-tier2",
                plan: SubscriptionPlan.Growth,
                cancellationToken);

            var demoTier1Tenant = await EnsureTenantAsync(
                dbContext,
                name: "Demo Starter",
                code: "demo-tier1",
                plan: SubscriptionPlan.Starter,
                cancellationToken);

            await CleanupTransientDemoUsersAsync(dbContext, demoTier3Tenant.Id, cancellationToken);

            var usersChanged = new List<bool>
            {
                await UpsertUserAsync(
                    dbContext, passwordHasher,
                    userName: "platform.admin",
                    email: "platform.admin@erp.local",
                    password: "Test123!",
                    role: AppRoles.Admin,
                    tenantId: null,
                    cancellationToken),

                await UpsertUserAsync(
                    dbContext, passwordHasher,
                    userName: "demo",
                    email: "demo@erp.local",
                    password: "Test123!",
                    role: AppRoles.Tier3,
                    tenantId: demoTier3Tenant.Id,
                    cancellationToken),

                await UpsertUserAsync(
                    dbContext, passwordHasher,
                    userName: "demo.tier2",
                    email: "demo.tier2@erp.local",
                    password: "Test123!",
                    role: AppRoles.Tier2,
                    tenantId: demoTier2Tenant.Id,
                    cancellationToken),

                await UpsertUserAsync(
                    dbContext, passwordHasher,
                    userName: "demo.tier1",
                    email: "demo.tier1@erp.local",
                    password: "Test123!",
                    role: AppRoles.Tier1,
                    tenantId: demoTier1Tenant.Id,
                    cancellationToken)
            };

            if (usersChanged.Any(x => x))
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Development users ensured: platform.admin, demo, demo.tier2, demo.tier1.");
            }
        }
        finally
        {
            SeedLock.Release();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Demo veri yükleme
    // ─────────────────────────────────────────────────────────────────────────

    private static async Task EnsureDemoDataAsync(
        ErpDbContext db,
        Guid tenantId,
        ILogger logger,
        CancellationToken ct)
    {
        // Daha önce yüklenmiş mi?
        if (await db.Companies.AnyAsync(x => x.TenantAccountId == tenantId, ct))
        {
            return;
        }

        logger.LogInformation("Demo verileri yükleniyor...");

        // ── Şirket / Şube / Depo ─────────────────────────────────────────────

        var company = new Company
        {
            TenantAccountId = tenantId,
            Code = "DEMO",
            Name = "Demokart Elektronik A.Ş.",
            TaxNumber = "1234567890"
        };

        var branch = new Branch
        {
            TenantAccountId = tenantId,
            CompanyId = company.Id,
            Code = "MRK",
            Name = "Merkez Şube"
        };

        var warehouse = new Warehouse
        {
            TenantAccountId = tenantId,
            BranchId = branch.Id,
            Code = "DEP01",
            Name = "Merkez Depo"
        };

        db.Companies.Add(company);
        db.Branches.Add(branch);
        db.Warehouses.Add(warehouse);

        // ── Ürünler (15 adet) ────────────────────────────────────────────────

        var p1  = Prod(tenantId, warehouse.Id, "PRD001", "Samsung 65\" QLED 4K TV",           "Elektronik",    "Televizyon",  "Samsung",  "8806094xxxxxx", 22000m, 27500m, 18, 0.20m);
        var p2  = Prod(tenantId, warehouse.Id, "PRD002", "LG Buzdolabı 600L NFC",              "Beyaz Eşya",   "Buzdolabı",   "LG",       "8806091xxxxxx", 15000m, 19900m, 10, 0.20m);
        var p3  = Prod(tenantId, warehouse.Id, "PRD003", "Apple iPhone 15 Pro 256GB",           "Telefon",      "Akıllı Telefon","Apple",  "190199xxxxxxx", 48000m, 59999m, 25, 0.20m);
        var p4  = Prod(tenantId, warehouse.Id, "PRD004", "Tefal ActiFry Airfryer 1.5kg",        "Küçük Ev",     "Airfryer",    "Tefal",    "345678xxxxxxx", 2800m,  3999m,  30, 0.20m);
        var p5  = Prod(tenantId, warehouse.Id, "PRD005", "Dyson V15 Detect Kablosuz Süpürge",   "Küçük Ev",     "Süpürge",     "Dyson",    "123456xxxxxxx", 10500m, 13500m, 15, 0.20m);
        var p6  = Prod(tenantId, warehouse.Id, "PRD006", "Bosch 10kg Çamaşır Makinesi",         "Beyaz Eşya",   "Çamaşır Makinesi","Bosch","4242xxxxxxxxx", 12000m, 15500m, 8,  0.20m);
        var p7  = Prod(tenantId, warehouse.Id, "PRD007", "Sony WH-1000XM5 Kulaklık",            "Ses Sistemleri","Kulaklık",   "Sony",     "4548736xxxxxx", 6500m,  8500m,  20, 0.20m);
        var p8  = Prod(tenantId, warehouse.Id, "PRD008", "Apple MacBook Pro M3 14\" 512GB",     "Bilgisayar",   "Dizüstü",     "Apple",    "195949xxxxxxx", 65000m, 79999m, 12, 0.20m);
        var p9  = Prod(tenantId, warehouse.Id, "PRD009", "Philips EP2224 Kahve Makinesi",        "Küçük Ev",     "Kahve Makinesi","Philips","8710103xxxxxx", 4200m,  5999m,  22, 0.20m);
        var p10 = Prod(tenantId, warehouse.Id, "PRD010", "Samsung Galaxy Watch 6 Classic 47mm", "Aksesuar",     "Akıllı Saat", "Samsung",  "8806094xxxxxx", 5500m,  7499m,  18, 0.20m);
        var p11 = Prod(tenantId, warehouse.Id, "PRD011", "Toshiba 1TB 2.5\" SSD SATA",          "Bilgisayar",   "Depolama",    "Toshiba",  "4547808xxxxxx", 1800m,  2499m,  40, 0.10m);
        var p12 = Prod(tenantId, warehouse.Id, "PRD012", "Logitech MX Keys Advanced Keyboard",  "Bilgisayar",   "Çevre Birimi","Logitech", "097855xxxxxxx", 1400m,  1999m,  35, 0.10m);
        var p13 = Prod(tenantId, warehouse.Id, "PRD013", "Canon EOS R50 Mirrorless Kit 18-45mm","Fotoğraf",     "Kamera",      "Canon",    "013803xxxxxxx", 23000m, 29999m, 10, 0.20m);
        var p14 = Prod(tenantId, warehouse.Id, "PRD014", "Beko DIN 28430 Bulaşık Makinesi",     "Beyaz Eşya",   "Bulaşık Makinesi","Beko", "8690842xxxxxx", 7800m,  9999m,  12, 0.20m);
        var p15 = Prod(tenantId, warehouse.Id, "PRD015", "Dell XPS 15 9530 i7 32GB 1TB",        "Bilgisayar",   "Dizüstü",     "Dell",     "884116xxxxxxx", 55000m, 69999m, 8,  0.20m);

        var products = new[] { p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15 };
        db.Products.AddRange(products);

        // ── Cari Hesaplar ─────────────────────────────────────────────────────

        var c1  = Cari(tenantId, "CAR001", "Mega Teknoloji Ltd. Şti.",         CariType.BuyerBch,    "0212 555 01 01", 150000m, 45);
        var c2  = Cari(tenantId, "CAR002", "Doğan Elektronik A.Ş.",            CariType.BuyerBch,    "0216 444 02 02", 200000m, 30);
        var c3  = Cari(tenantId, "CAR003", "Güneş Ticaret ve Sanayi Ltd.",     CariType.BuyerBch,    "0232 333 03 03", 80000m,  30);
        var c4  = Cari(tenantId, "CAR004", "Aksoy Mağazacılık Ltd. Şti.",      CariType.BuyerBch,    "0312 222 04 04", 120000m, 45);
        var c5  = Cari(tenantId, "CAR005", "Çelik Perakende Zinciri A.Ş.",     CariType.BuyerBch,    "0224 111 05 05", 250000m, 60);
        var c6  = Cari(tenantId, "CAR006", "Koç Distribütörlük A.Ş.",          CariType.Supplier, "0212 888 06 06", 500000m, 30);
        var c7  = Cari(tenantId, "CAR007", "Vestel Distribüsyon Ltd. Şti.",    CariType.Supplier, "0222 777 07 07", 300000m, 30);
        var c8  = Cari(tenantId, "CAR008", "Global Tedarik ve Lojistik Ltd.",  CariType.Supplier, "0216 666 08 08", 400000m, 45);
        var c9  = Cari(tenantId, "CAR009", "Yıldız Holding A.Ş.",             CariType.Both,     "0212 999 09 09", 350000m, 30);

        var cariAccounts = new[] { c1, c2, c3, c4, c5, c6, c7, c8, c9 };
        db.CariAccounts.AddRange(cariAccounts);

        // ── Satış Siparişleri (12 adet) ───────────────────────────────────────

        var so1  = SO(tenantId, "SS-2026-001", c1.Id, warehouse.Id, DateTime.UtcNow.AddDays(-30), OrderStatus.Approved,
            (p3.Id, 5m, 59999m), (p7.Id, 3m, 8500m));
        var so2  = SO(tenantId, "SS-2026-002", c2.Id, warehouse.Id, DateTime.UtcNow.AddDays(-28), OrderStatus.Approved,
            (p1.Id, 2m, 27500m), (p8.Id, 1m, 79999m));
        var so3  = SO(tenantId, "SS-2026-003", c3.Id, warehouse.Id, DateTime.UtcNow.AddDays(-25), OrderStatus.Approved,
            (p4.Id, 10m, 3999m), (p9.Id, 5m, 5999m), (p12.Id, 8m, 1999m));
        var so4  = SO(tenantId, "SS-2026-004", c4.Id, warehouse.Id, DateTime.UtcNow.AddDays(-22), OrderStatus.Approved,
            (p2.Id, 3m, 19900m), (p14.Id, 2m, 9999m));
        var so5  = SO(tenantId, "SS-2026-005", c5.Id, warehouse.Id, DateTime.UtcNow.AddDays(-20), OrderStatus.Approved,
            (p15.Id, 2m, 69999m), (p8.Id, 3m, 79999m), (p3.Id, 4m, 59999m));
        var so6  = SO(tenantId, "SS-2026-006", c1.Id, warehouse.Id, DateTime.UtcNow.AddDays(-18), OrderStatus.Approved,
            (p13.Id, 2m, 29999m), (p10.Id, 4m, 7499m));
        var so7  = SO(tenantId, "SS-2026-007", c2.Id, warehouse.Id, DateTime.UtcNow.AddDays(-15), OrderStatus.Approved,
            (p5.Id, 5m, 13500m), (p6.Id, 3m, 15500m));
        var so8  = SO(tenantId, "SS-2026-008", c3.Id, warehouse.Id, DateTime.UtcNow.AddDays(-12), OrderStatus.Draft,
            (p11.Id, 20m, 2499m), (p12.Id, 15m, 1999m));
        var so9  = SO(tenantId, "SS-2026-009", c4.Id, warehouse.Id, DateTime.UtcNow.AddDays(-10), OrderStatus.Draft,
            (p3.Id, 3m, 59999m), (p7.Id, 2m, 8500m));
        var so10 = SO(tenantId, "SS-2026-010", c5.Id, warehouse.Id, DateTime.UtcNow.AddDays(-8),  OrderStatus.Approved,
            (p1.Id, 4m, 27500m), (p2.Id, 2m, 19900m));
        var so11 = SO(tenantId, "SS-2026-011", c1.Id, warehouse.Id, DateTime.UtcNow.AddDays(-5),  OrderStatus.Cancelled,
            (p8.Id, 1m, 79999m));
        var so12 = SO(tenantId, "SS-2026-012", c9.Id, warehouse.Id, DateTime.UtcNow.AddDays(-2),  OrderStatus.Draft,
            (p4.Id, 6m, 3999m), (p9.Id, 4m, 5999m), (p10.Id, 3m, 7499m));

        var salesOrders = new[] { so1, so2, so3, so4, so5, so6, so7, so8, so9, so10, so11, so12 };
        foreach (var so in salesOrders)
        {
            db.SalesOrders.Add(so);
            db.SalesOrderItems.AddRange(so.Items);
        }

        // ── Satın Alma Siparişleri (6 adet) ──────────────────────────────────

        var po1 = PO(tenantId, "AS-2026-001", c6.Id, warehouse.Id, DateTime.UtcNow.AddDays(-35), OrderStatus.Approved,
            (p3.Id, 20m, 48000m), (p8.Id, 5m, 65000m), (p10.Id, 10m, 5500m));
        var po2 = PO(tenantId, "AS-2026-002", c7.Id, warehouse.Id, DateTime.UtcNow.AddDays(-30), OrderStatus.Approved,
            (p1.Id, 10m, 22000m), (p2.Id, 8m, 15000m), (p6.Id, 6m, 12000m));
        var po3 = PO(tenantId, "AS-2026-003", c8.Id, warehouse.Id, DateTime.UtcNow.AddDays(-25), OrderStatus.Approved,
            (p4.Id, 30m, 2800m), (p5.Id, 10m, 10500m), (p9.Id, 15m, 4200m));
        var po4 = PO(tenantId, "AS-2026-004", c6.Id, warehouse.Id, DateTime.UtcNow.AddDays(-15), OrderStatus.Approved,
            (p13.Id, 5m, 23000m), (p15.Id, 3m, 55000m));
        var po5 = PO(tenantId, "AS-2026-005", c7.Id, warehouse.Id, DateTime.UtcNow.AddDays(-10), OrderStatus.Draft,
            (p7.Id, 15m, 6500m), (p11.Id, 50m, 1800m), (p12.Id, 40m, 1400m));
        var po6 = PO(tenantId, "AS-2026-006", c9.Id, warehouse.Id, DateTime.UtcNow.AddDays(-3),  OrderStatus.Draft,
            (p14.Id, 8m, 7800m), (p2.Id, 4m, 15000m));

        var purchaseOrders = new[] { po1, po2, po3, po4, po5, po6 };
        foreach (var po in purchaseOrders)
        {
            db.PurchaseOrders.Add(po);
            db.PurchaseOrderItems.AddRange(po.Items);
        }

        // ── Stok Hareketleri (23 adet) ────────────────────────────────────────

        // Giriş hareketleri (satın almalardan)
        var smList = new List<StockMovement>
        {
            SM(tenantId, warehouse.Id, p3.Id,  StockMovementType.In,  20m, 48000m, DateTime.UtcNow.AddDays(-35), "GS-PO-001"),
            SM(tenantId, warehouse.Id, p8.Id,  StockMovementType.In,  5m,  65000m, DateTime.UtcNow.AddDays(-35), "GS-PO-001"),
            SM(tenantId, warehouse.Id, p10.Id, StockMovementType.In,  10m, 5500m,  DateTime.UtcNow.AddDays(-35), "GS-PO-001"),
            SM(tenantId, warehouse.Id, p1.Id,  StockMovementType.In,  10m, 22000m, DateTime.UtcNow.AddDays(-30), "GS-PO-002"),
            SM(tenantId, warehouse.Id, p2.Id,  StockMovementType.In,  8m,  15000m, DateTime.UtcNow.AddDays(-30), "GS-PO-002"),
            SM(tenantId, warehouse.Id, p6.Id,  StockMovementType.In,  6m,  12000m, DateTime.UtcNow.AddDays(-30), "GS-PO-002"),
            SM(tenantId, warehouse.Id, p4.Id,  StockMovementType.In,  30m, 2800m,  DateTime.UtcNow.AddDays(-25), "GS-PO-003"),
            SM(tenantId, warehouse.Id, p5.Id,  StockMovementType.In,  10m, 10500m, DateTime.UtcNow.AddDays(-25), "GS-PO-003"),
            SM(tenantId, warehouse.Id, p9.Id,  StockMovementType.In,  15m, 4200m,  DateTime.UtcNow.AddDays(-25), "GS-PO-003"),
            SM(tenantId, warehouse.Id, p13.Id, StockMovementType.In,  5m,  23000m, DateTime.UtcNow.AddDays(-15), "GS-PO-004"),
            SM(tenantId, warehouse.Id, p15.Id, StockMovementType.In,  3m,  55000m, DateTime.UtcNow.AddDays(-15), "GS-PO-004"),
            SM(tenantId, warehouse.Id, p11.Id, StockMovementType.In,  50m, 1800m,  DateTime.UtcNow.AddDays(-8),  "GS-PO-005"),
            SM(tenantId, warehouse.Id, p12.Id, StockMovementType.In,  40m, 1400m,  DateTime.UtcNow.AddDays(-8),  "GS-PO-005"),

            // Çıkış hareketleri (satışlardan)
            SM(tenantId, warehouse.Id, p3.Id,  StockMovementType.Out, 5m,  59999m, DateTime.UtcNow.AddDays(-30), "CS-SO-001"),
            SM(tenantId, warehouse.Id, p7.Id,  StockMovementType.Out, 3m,  8500m,  DateTime.UtcNow.AddDays(-30), "CS-SO-001"),
            SM(tenantId, warehouse.Id, p1.Id,  StockMovementType.Out, 2m,  27500m, DateTime.UtcNow.AddDays(-28), "CS-SO-002"),
            SM(tenantId, warehouse.Id, p8.Id,  StockMovementType.Out, 1m,  79999m, DateTime.UtcNow.AddDays(-28), "CS-SO-002"),
            SM(tenantId, warehouse.Id, p4.Id,  StockMovementType.Out, 10m, 3999m,  DateTime.UtcNow.AddDays(-25), "CS-SO-003"),
            SM(tenantId, warehouse.Id, p9.Id,  StockMovementType.Out, 5m,  5999m,  DateTime.UtcNow.AddDays(-25), "CS-SO-003"),
            SM(tenantId, warehouse.Id, p2.Id,  StockMovementType.Out, 3m,  19900m, DateTime.UtcNow.AddDays(-22), "CS-SO-004"),
            SM(tenantId, warehouse.Id, p15.Id, StockMovementType.Out, 2m,  69999m, DateTime.UtcNow.AddDays(-20), "CS-SO-005"),
            SM(tenantId, warehouse.Id, p13.Id, StockMovementType.Out, 2m,  29999m, DateTime.UtcNow.AddDays(-18), "CS-SO-006"),
            SM(tenantId, warehouse.Id, p5.Id,  StockMovementType.Out, 5m,  13500m, DateTime.UtcNow.AddDays(-15), "CS-SO-007"),
        };

        db.StockMovements.AddRange(smList);

        // ── Finans Hareketleri (12 adet) ─────────────────────────────────────

        var fmList = new List<FinanceMovement>
        {
            FM(tenantId, c1.Id, FinanceMovementType.Collection, 354992m,  DateTime.UtcNow.AddDays(-28), "SO-001 tahsilat",    "TAH-001"),
            FM(tenantId, c2.Id, FinanceMovementType.Collection, 134999m,  DateTime.UtcNow.AddDays(-25), "SO-002 tahsilat",    "TAH-002"),
            FM(tenantId, c3.Id, FinanceMovementType.Collection, 69982m,   DateTime.UtcNow.AddDays(-22), "SO-003 tahsilat",    "TAH-003"),
            FM(tenantId, c4.Id, FinanceMovementType.Collection, 79697m,   DateTime.UtcNow.AddDays(-20), "SO-004 tahsilat",    "TAH-004"),
            FM(tenantId, c5.Id, FinanceMovementType.Collection, 479991m,  DateTime.UtcNow.AddDays(-18), "SO-005 tahsilat",    "TAH-005"),
            FM(tenantId, c1.Id, FinanceMovementType.Collection, 89996m,   DateTime.UtcNow.AddDays(-16), "SO-006 kısmi ödeme", "TAH-006"),
            FM(tenantId, c6.Id, FinanceMovementType.Payment,    960000m,  DateTime.UtcNow.AddDays(-33), "PO-001 ödeme",       "ODE-001"),
            FM(tenantId, c7.Id, FinanceMovementType.Payment,    382000m,  DateTime.UtcNow.AddDays(-28), "PO-002 ödeme",       "ODE-002"),
            FM(tenantId, c8.Id, FinanceMovementType.Payment,    231000m,  DateTime.UtcNow.AddDays(-23), "PO-003 ödeme",       "ODE-003"),
            FM(tenantId, c6.Id, FinanceMovementType.Payment,    280000m,  DateTime.UtcNow.AddDays(-13), "PO-004 ödeme",       "ODE-004"),
            FM(tenantId, c2.Id, FinanceMovementType.Collection, 215997m,  DateTime.UtcNow.AddDays(-12), "SO-007 tahsilat",    "TAH-007"),
            FM(tenantId, c5.Id, FinanceMovementType.Collection, 220000m,  DateTime.UtcNow.AddDays(-6),  "SO-010 avans",       "TAH-008"),
        };

        db.FinanceMovements.AddRange(fmList);

        // ── Faturalar (10 adet) ───────────────────────────────────────────────

        var inv1 = Inv(tenantId, "FTR-2026-001", InvoiceType.EArsiv, InvoiceCategory.Satis,
            InvoiceStatus.Approved, c1.Id, c1.Name, "1111111111",
            so1.Id, null, DateTime.UtcNow.AddDays(-29),
            new[] {
                InvItem(tenantId, p3.Id, p3.Name, p3.BarcodeEan13 ?? "", 5m, "EA", 59999m, 0m, 0.20m),
                InvItem(tenantId, p7.Id, p7.Name, p7.BarcodeEan13 ?? "", 3m, "EA", 8500m,  0m, 0.20m),
            });

        var inv2 = Inv(tenantId, "FTR-2026-002", InvoiceType.EFatura, InvoiceCategory.Satis,
            InvoiceStatus.Approved, c2.Id, c2.Name, "2222222222",
            so2.Id, null, DateTime.UtcNow.AddDays(-27),
            new[] {
                InvItem(tenantId, p1.Id, p1.Name, p1.BarcodeEan13 ?? "", 2m, "EA", 27500m, 0m, 0.20m),
                InvItem(tenantId, p8.Id, p8.Name, p8.BarcodeEan13 ?? "", 1m, "EA", 79999m, 0m, 0.20m),
            });

        var inv3 = Inv(tenantId, "FTR-2026-003", InvoiceType.EArsiv, InvoiceCategory.Satis,
            InvoiceStatus.Approved, c3.Id, c3.Name, "3333333333",
            so3.Id, null, DateTime.UtcNow.AddDays(-24),
            new[] {
                InvItem(tenantId, p4.Id, p4.Name, p4.BarcodeEan13 ?? "", 10m, "EA", 3999m, 0m, 0.20m),
                InvItem(tenantId, p9.Id, p9.Name, p9.BarcodeEan13 ?? "", 5m,  "EA", 5999m, 0m, 0.20m),
                InvItem(tenantId, p12.Id, p12.Name, p12.BarcodeEan13 ?? "", 8m,"EA", 1999m, 0m, 0.10m),
            });

        var inv4 = Inv(tenantId, "FTR-2026-004", InvoiceType.EFatura, InvoiceCategory.Satis,
            InvoiceStatus.Approved, c4.Id, c4.Name, "4444444444",
            so4.Id, null, DateTime.UtcNow.AddDays(-21),
            new[] {
                InvItem(tenantId, p2.Id, p2.Name, p2.BarcodeEan13 ?? "", 3m, "EA", 19900m, 0m, 0.20m),
                InvItem(tenantId, p14.Id, p14.Name, p14.BarcodeEan13 ?? "", 2m,"EA", 9999m,  0m, 0.20m),
            });

        var inv5 = Inv(tenantId, "FTR-2026-005", InvoiceType.EFatura, InvoiceCategory.Satis,
            InvoiceStatus.Approved, c5.Id, c5.Name, "5555555555",
            so5.Id, null, DateTime.UtcNow.AddDays(-19),
            new[] {
                InvItem(tenantId, p15.Id, p15.Name, p15.BarcodeEan13 ?? "", 2m, "EA", 69999m, 0m, 0.20m),
                InvItem(tenantId, p8.Id,  p8.Name,  p8.BarcodeEan13 ?? "", 3m,  "EA", 79999m, 0m, 0.20m),
                InvItem(tenantId, p3.Id,  p3.Name,  p3.BarcodeEan13 ?? "", 4m,  "EA", 59999m, 0m, 0.20m),
            });

        var inv6 = Inv(tenantId, "FTR-2026-006", InvoiceType.EArsiv, InvoiceCategory.Satis,
            InvoiceStatus.Sent, c1.Id, c1.Name, "1111111111",
            so6.Id, null, DateTime.UtcNow.AddDays(-17),
            new[] {
                InvItem(tenantId, p13.Id, p13.Name, p13.BarcodeEan13 ?? "", 2m, "EA", 29999m, 0m, 0.20m),
                InvItem(tenantId, p10.Id, p10.Name, p10.BarcodeEan13 ?? "", 4m, "EA", 7499m,  0m, 0.20m),
            });

        var inv7 = Inv(tenantId, "FTR-2026-007", InvoiceType.EFatura, InvoiceCategory.Satis,
            InvoiceStatus.Sent, c2.Id, c2.Name, "2222222222",
            so7.Id, null, DateTime.UtcNow.AddDays(-14),
            new[] {
                InvItem(tenantId, p5.Id, p5.Name, p5.BarcodeEan13 ?? "", 5m, "EA", 13500m, 0m, 0.20m),
                InvItem(tenantId, p6.Id, p6.Name, p6.BarcodeEan13 ?? "", 3m, "EA", 15500m, 0m, 0.20m),
            });

        // Alış faturaları (tedarikçilerden)
        var inv8 = Inv(tenantId, "FTR-2026-008", InvoiceType.EFatura, InvoiceCategory.Alis,
            InvoiceStatus.Approved, c6.Id, c6.Name, "6666666666",
            null, po1.Id, DateTime.UtcNow.AddDays(-34),
            new[] {
                InvItem(tenantId, p3.Id,  p3.Name,  p3.BarcodeEan13 ?? "", 20m, "EA", 48000m, 0m, 0.20m),
                InvItem(tenantId, p8.Id,  p8.Name,  p8.BarcodeEan13 ?? "", 5m,  "EA", 65000m, 0m, 0.20m),
                InvItem(tenantId, p10.Id, p10.Name, p10.BarcodeEan13 ?? "", 10m,"EA", 5500m,  0m, 0.20m),
            });

        var inv9 = Inv(tenantId, "FTR-2026-009", InvoiceType.EFatura, InvoiceCategory.Alis,
            InvoiceStatus.Approved, c7.Id, c7.Name, "7777777777",
            null, po2.Id, DateTime.UtcNow.AddDays(-29),
            new[] {
                InvItem(tenantId, p1.Id, p1.Name, p1.BarcodeEan13 ?? "", 10m, "EA", 22000m, 0m, 0.20m),
                InvItem(tenantId, p2.Id, p2.Name, p2.BarcodeEan13 ?? "", 8m,  "EA", 15000m, 0m, 0.20m),
                InvItem(tenantId, p6.Id, p6.Name, p6.BarcodeEan13 ?? "", 6m,  "EA", 12000m, 0m, 0.20m),
            });

        // İade faturası
        var inv10 = Inv(tenantId, "FTR-2026-010", InvoiceType.EArsiv, InvoiceCategory.Iade,
            InvoiceStatus.Approved, c1.Id, c1.Name, "1111111111",
            null, null, DateTime.UtcNow.AddDays(-10),
            new[] {
                InvItem(tenantId, p8.Id, p8.Name, p8.BarcodeEan13 ?? "", 1m, "EA", 79999m, 0m, 0.20m),
            });

        var invoices = new[] { inv1, inv2, inv3, inv4, inv5, inv6, inv7, inv8, inv9, inv10 };
        foreach (var inv in invoices)
        {
            db.Invoices.Add(inv);
            db.InvoiceItems.AddRange(inv.Items);
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Demo verileri başarıyla yüklendi: 15 ürün, 9 cari, 12 satış siparişi, 6 alım siparişi, 23 stok hareketi, 12 finans hareketi, 10 fatura.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Factory yardımcıları
    // ─────────────────────────────────────────────────────────────────────────

    private static Product Prod(
        Guid tenantId, Guid warehouseId,
        string code, string name, string category, string? subCategory,
        string brand, string barcode,
        decimal purchasePrice, decimal salePrice,
        decimal minStock, decimal vatRate) => new()
    {
        TenantAccountId     = tenantId,
        Code                = code,
        Name                = name,
        Category            = category,
        SubCategory         = subCategory,
        Brand               = brand,
        BarcodeEan13        = barcode,
        Unit                = "EA",
        DefaultSalePrice    = salePrice,
        LastPurchasePrice   = purchasePrice,
        LastSalePrice       = salePrice,
        MinimumStockLevel   = minStock,
        CriticalStockLevel  = Math.Floor(minStock / 2),
        PurchaseVatRate     = vatRate,
        SalesVatRate        = vatRate,
        DefaultWarehouseId  = warehouseId,
        IsActive            = true
    };

    private static CariAccount Cari(
        Guid tenantId, string code, string name, CariType type,
        string phone, decimal riskLimit, int maturityDays) => new()
    {
        TenantAccountId = tenantId,
        Code            = code,
        Name            = name,
        Type            = type,
        Phone           = phone,
        RiskLimit       = riskLimit,
        MaturityDays    = maturityDays,
        CurrentBalance  = 0m
    };

    private static SalesOrder SO(
        Guid tenantId, string orderNo, Guid customerId, Guid warehouseId,
        DateTime date, OrderStatus status,
        params (Guid ProductId, decimal Qty, decimal UnitPrice)[] items)
    {
        var order = new SalesOrder
        {
            TenantAccountId      = tenantId,
            OrderNo              = orderNo,
            CustomerCariAccountId = customerId,
            WarehouseId          = warehouseId,
            OrderDateUtc         = date,
            Status               = status
        };

        foreach (var (productId, qty, price) in items)
        {
            order.Items.Add(new SalesOrderItem
            {
                TenantAccountId = tenantId,
                SalesOrderId    = order.Id,
                ProductId       = productId,
                Quantity        = qty,
                UnitPrice       = price
            });
        }

        return order;
    }

    private static PurchaseOrder PO(
        Guid tenantId, string orderNo, Guid supplierId, Guid warehouseId,
        DateTime date, OrderStatus status,
        params (Guid ProductId, decimal Qty, decimal UnitPrice)[] items)
    {
        var order = new PurchaseOrder
        {
            TenantAccountId        = tenantId,
            OrderNo                = orderNo,
            SupplierCariAccountId  = supplierId,
            WarehouseId            = warehouseId,
            OrderDateUtc           = date,
            Status                 = status
        };

        foreach (var (productId, qty, price) in items)
        {
            order.Items.Add(new PurchaseOrderItem
            {
                TenantAccountId  = tenantId,
                PurchaseOrderId  = order.Id,
                ProductId        = productId,
                Quantity         = qty,
                UnitPrice        = price
            });
        }

        return order;
    }

    private static StockMovement SM(
        Guid tenantId, Guid warehouseId, Guid productId,
        StockMovementType type, decimal qty, decimal unitPrice,
        DateTime date, string refNo) => new()
    {
        TenantAccountId  = tenantId,
        WarehouseId      = warehouseId,
        ProductId        = productId,
        Type             = type,
        Quantity         = qty,
        UnitPrice        = unitPrice,
        MovementDateUtc  = date,
        ReferenceNo      = refNo
    };

    private static FinanceMovement FM(
        Guid tenantId, Guid cariId, FinanceMovementType type,
        decimal amount, DateTime date, string desc, string refNo) => new()
    {
        TenantAccountId = tenantId,
        CariAccountId   = cariId,
        Type            = type,
        Amount          = amount,
        MovementDateUtc = date,
        Description     = desc,
        ReferenceNo     = refNo
    };

    private static Invoice Inv(
        Guid tenantId, string number, InvoiceType type, InvoiceCategory category,
        InvoiceStatus status, Guid cariId, string cariName, string taxNumber,
        Guid? salesOrderId, Guid? purchaseOrderId, DateTime issueDate,
        InvoiceItem[] items)
    {
        var subtotal      = items.Sum(i => i.Quantity * i.UnitPrice * (1 - i.DiscountRate));
        var taxTotal      = items.Sum(i => i.TaxAmount);
        var discountTotal = items.Sum(i => i.DiscountAmount);
        var grandTotal    = subtotal + taxTotal - discountTotal;

        var inv = new Invoice
        {
            TenantAccountId  = tenantId,
            InvoiceNumber    = number,
            InvoiceType      = type,
            InvoiceCategory  = category,
            Status           = status,
            CariAccountId    = cariId,
            CariAccountName  = cariName,
            TaxNumber        = taxNumber,
            SalesOrderId     = salesOrderId,
            PurchaseOrderId  = purchaseOrderId,
            IssueDateUtc     = issueDate,
            DueDateUtc       = issueDate.AddDays(30),
            Subtotal         = subtotal,
            TaxTotal         = taxTotal,
            DiscountTotal    = discountTotal,
            GrandTotal       = grandTotal,
            Currency         = "TRY"
        };

        foreach (var item in items)
        {
            item.InvoiceId = inv.Id;
        }

        inv.Items = items;
        return inv;
    }

    private static InvoiceItem InvItem(
        Guid tenantId, Guid productId, string productName, string barcode,
        decimal qty, string unit, decimal unitPrice, decimal discountRate, decimal taxRate)
    {
        var discountAmount = qty * unitPrice * discountRate;
        var taxableAmount  = qty * unitPrice - discountAmount;
        var taxAmount      = taxableAmount * taxRate;
        var lineTotal      = taxableAmount + taxAmount;

        return new InvoiceItem
        {
            TenantAccountId = tenantId,
            ProductId       = productId,
            ProductName     = productName,
            Barcode         = barcode,
            Quantity        = qty,
            Unit            = unit,
            UnitPrice       = unitPrice,
            DiscountRate    = discountRate,
            DiscountAmount  = discountAmount,
            TaxRate         = taxRate,
            TaxAmount       = taxAmount,
            LineTotal       = lineTotal
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Tenant / User / Plan / Landing helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static async Task<TenantAccount> EnsureTenantAsync(
        ErpDbContext dbContext,
        string name, string code, SubscriptionPlan plan,
        CancellationToken cancellationToken)
    {
        var targetMaxUsers = SubscriptionPlanCatalog.GetMaxUsers(plan);
        var tenant = await dbContext.TenantAccounts
            .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);

        if (tenant is null)
        {
            tenant = new TenantAccount
            {
                Name                    = name,
                Code                    = code,
                Plan                    = plan,
                SubscriptionStatus      = SubscriptionStatus.Active,
                SubscriptionStartAtUtc  = DateTime.UtcNow,
                SubscriptionEndAtUtc    = DateTime.UtcNow.AddYears(1),
                MaxUsers                = targetMaxUsers
            };

            dbContext.TenantAccounts.Add(tenant);
            await dbContext.SaveChangesAsync(cancellationToken);
            return tenant;
        }

        var changed = false;
        if (!string.Equals(tenant.Name, name, StringComparison.Ordinal))
        {
            tenant.Name = name;
            changed = true;
        }

        if (tenant.Plan != plan)
        {
            tenant.Plan = plan;
            changed = true;
        }

        if (tenant.MaxUsers != targetMaxUsers)
        {
            tenant.MaxUsers = targetMaxUsers;
            changed = true;
        }

        if (tenant.SubscriptionStatus != SubscriptionStatus.Active)
        {
            tenant.SubscriptionStatus = SubscriptionStatus.Active;
            changed = true;
        }

        if (tenant.SubscriptionEndAtUtc is null || tenant.SubscriptionEndAtUtc <= DateTime.UtcNow)
        {
            tenant.SubscriptionEndAtUtc = DateTime.UtcNow.AddYears(1);
            changed = true;
        }

        if (changed)
        {
            tenant.UpdatedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return tenant;
    }

    private static async Task<bool> UpsertUserAsync(
        ErpDbContext dbContext,
        IPasswordHasher passwordHasher,
        string userName, string email, string password, string role,
        Guid? tenantId,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.Users
            .FirstOrDefaultAsync(
                x => x.UserName == userName || x.Email == email,
                cancellationToken);

        if (existing is null)
        {
            dbContext.Users.Add(new AppUser
            {
                TenantAccountId = tenantId,
                UserName        = userName,
                Email           = email,
                PasswordHash    = passwordHasher.Hash(password),
                Role            = role
            });
            return true;
        }

        var changed = false;

        if (!string.Equals(existing.Email, email, StringComparison.OrdinalIgnoreCase))
        { existing.Email = email; changed = true; }

        if (!string.Equals(existing.UserName, userName, StringComparison.OrdinalIgnoreCase))
        { existing.UserName = userName; changed = true; }

        if (!string.Equals(existing.Role, role, StringComparison.OrdinalIgnoreCase))
        { existing.Role = role; changed = true; }

        if (existing.TenantAccountId != tenantId)
        { existing.TenantAccountId = tenantId; changed = true; }

        if (!passwordHasher.Verify(password, existing.PasswordHash))
        { existing.PasswordHash = passwordHasher.Hash(password); changed = true; }

        if (changed) existing.UpdatedAtUtc = DateTime.UtcNow;

        return changed;
    }

    private static async Task CleanupTransientDemoUsersAsync(
        ErpDbContext dbContext,
        Guid demoTier3TenantId,
        CancellationToken cancellationToken)
    {
        var junkUsers = await dbContext.Users
            .Where(x =>
                x.TenantAccountId == demoTier3TenantId
                && EF.Functions.Like(x.UserName, "demo[_]%")
                && EF.Functions.Like(x.Email, "demo[_]%@example.com"))
            .ToListAsync(cancellationToken);

        if (junkUsers.Count == 0)
        {
            return;
        }

        var junkUserIds = junkUsers.Select(x => x.Id).ToList();

        var sessions = await dbContext.UserSessions
            .Where(x => junkUserIds.Contains(x.UserId))
            .ToListAsync(cancellationToken);

        if (sessions.Count > 0)
        {
            dbContext.UserSessions.RemoveRange(sessions);
        }

        var activityLogs = await dbContext.SystemActivityLogs
            .Where(x => x.UserId.HasValue && junkUserIds.Contains(x.UserId.Value))
            .ToListAsync(cancellationToken);

        foreach (var log in activityLogs)
        {
            log.UserId = null;
            log.UpdatedAtUtc = DateTime.UtcNow;
        }

        dbContext.Users.RemoveRange(junkUsers);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureDefaultPlanSettingsAsync(
        ErpDbContext dbContext, CancellationToken cancellationToken)
    {
        var existingPlans = await dbContext.SubscriptionPlanSettings
            .AsNoTracking()
            .Select(x => x.Plan)
            .ToListAsync(cancellationToken);

        var missing = Enum.GetValues<SubscriptionPlan>()
            .Where(plan => !existingPlans.Contains(plan))
            .ToList();

        if (missing.Count == 0) return;

        foreach (var plan in missing)
        {
            dbContext.SubscriptionPlanSettings.Add(new SubscriptionPlanSetting
            {
                Plan         = plan,
                DisplayName  = SubscriptionPlanCatalog.GetDisplayName(plan),
                MonthlyPrice = SubscriptionPlanCatalog.GetDefaultMonthlyPrice(plan),
                MaxUsers     = SubscriptionPlanCatalog.GetMaxUsers(plan),
                FeaturesCsv  = string.Join(',', SubscriptionPlanCatalog.GetFeatures(plan)),
                IsActive     = true
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureDefaultLandingContentAsync(
        ErpDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.LandingPageContents.AnyAsync(cancellationToken)) return;

        var defaults = new List<LandingPageContent>
        {
            new() { Key = "hero-title",    Title = "Hero Title",    Content = "Perakende satisinizi tek panelden yonetin.",      SortOrder = 1, IsPublished = true },
            new() { Key = "hero-subtitle", Title = "Hero Subtitle", Content = "Stok, cari, pos ve raporlar tek bir ERP platformunda.", SortOrder = 2, IsPublished = true },
            new() { Key = "pricing-note",  Title = "Pricing Note",  Content = "Abonelik paketleri aylik olarak yenilenir.",      SortOrder = 3, IsPublished = true }
        };

        dbContext.LandingPageContents.AddRange(defaults);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureDefaultEmailTemplatesAsync(
        ErpDbContext dbContext, CancellationToken cancellationToken)
    {
        var defaults = new List<PlatformEmailTemplate>
        {
            new()
            {
                Key = "welcome",
                Name = "Welcome Mail",
                SubjectTemplate = "Hos geldiniz {{TenantName}}",
                BodyTemplate = "<p>Merhaba {{TenantName}},</p><p>ERP platformuna hos geldiniz. Tenant kodunuz: <strong>{{TenantCode}}</strong>.</p><p>Tarih: {{NowUtc}} UTC</p>",
                Description = "Yeni abone olan tenantlara gonderilen karsilama e-postasi.",
                IsActive = true
            },
            new()
            {
                Key = "invoice",
                Name = "Invoice Mail",
                SubjectTemplate = "Abonelik fatura bilgilendirmesi - {{TenantName}}",
                BodyTemplate = "<p>Merhaba {{TenantName}},</p><p>Abonelik planiniz: <strong>{{Plan}}</strong>. Durum: {{SubscriptionStatus}}.</p><p>Son odeme tarihi: <strong>{{SubscriptionEndDate}}</strong></p>",
                Description = "Fatura veya plan bilgilendirme e-postasi.",
                IsActive = true
            },
            new()
            {
                Key = "reminder",
                Name = "Reminder Mail",
                SubjectTemplate = "Hatirlatma - {{TenantName}}",
                BodyTemplate = "<p>Merhaba {{TenantName}},</p><p>Bu bir hatirlatma e-postasidir.</p><p>Tarih: {{NowUtc}} UTC</p>",
                Description = "Genel hatirlatma e-postasi.",
                IsActive = true
            }
        };

        foreach (var template in defaults)
        {
            var existing = await dbContext.PlatformEmailTemplates.FirstOrDefaultAsync(x => x.Key == template.Key, cancellationToken);
            if (existing is null)
            {
                dbContext.PlatformEmailTemplates.Add(template);
                continue;
            }

            existing.Name = template.Name;
            existing.SubjectTemplate = template.SubjectTemplate;
            existing.BodyTemplate = template.BodyTemplate;
            existing.Description = template.Description;
            existing.IsActive = template.IsActive;
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
