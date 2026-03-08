using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository : EfRepository<Product>, IProductRepository
{
    private readonly ErpDbContext _dbContext;

    public ProductRepository(ErpDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Product?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products.FirstOrDefaultAsync(
            x => x.Code.ToLower() == code.ToLower(),
            cancellationToken);
    }

    public async Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeBarcode(barcode);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return await _dbContext.Products.FirstOrDefaultAsync(
            x => x.Code.ToLower() == normalized.ToLower()
                || (x.BarcodeEan13 != null && x.BarcodeEan13.ToLower() == normalized.ToLower())
                || (x.QrCode != null && x.QrCode.ToLower() == normalized.ToLower()),
            cancellationToken);
    }

    private static string NormalizeBarcode(string barcode)
    {
        return barcode.Trim();
    }
}
