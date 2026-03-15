using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Common;
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
        var normalized = NormalizeText(code);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return await _dbContext.Products.FirstOrDefaultAsync(
            x => x.Code.ToLower() == normalized.ToLower(),
            cancellationToken);
    }

    public async Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeText(barcode);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var directMatch = await _dbContext.Products.FirstOrDefaultAsync(
            x => x.Code.ToLower() == normalized.ToLower()
                || (x.BarcodeEan13 != null && x.BarcodeEan13.ToLower() == normalized.ToLower())
                || (x.QrCode != null && x.QrCode.ToLower() == normalized.ToLower()),
            cancellationToken);

        if (directMatch is not null)
        {
            return directMatch;
        }

        var candidateMatches = await _dbContext.Products
            .AsNoTracking()
            .Where(x => x.AlternativeBarcodesCsv != null && x.AlternativeBarcodesCsv.Contains(normalized))
            .ToListAsync(cancellationToken);

        return candidateMatches.FirstOrDefault(x => CsvListSerializer.ContainsToken(x.AlternativeBarcodesCsv, normalized));
    }

    public async Task<IReadOnlyList<Product>> SearchAsync(
        string? search,
        int page,
        int pageSize,
        string? sortBy,
        string sortDir,
        CancellationToken cancellationToken = default)
    {
        var normalizedSearch = NormalizeText(search);
        var safePage = page <= 0 ? 1 : page;
        var safePageSize = pageSize <= 0 ? 50 : Math.Min(pageSize, 200);

        var query = _dbContext.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(x =>
                x.Code.Contains(normalizedSearch)
                || x.Name.Contains(normalizedSearch)
                || x.Category.Contains(normalizedSearch)
                || (x.ShortDescription != null && x.ShortDescription.Contains(normalizedSearch))
                || (x.SubCategory != null && x.SubCategory.Contains(normalizedSearch))
                || (x.Brand != null && x.Brand.Contains(normalizedSearch))
                || (x.ProductType != null && x.ProductType.Contains(normalizedSearch))
                || (x.BarcodeEan13 != null && x.BarcodeEan13.Contains(normalizedSearch))
                || (x.QrCode != null && x.QrCode.Contains(normalizedSearch))
                || (x.AlternativeBarcodesCsv != null && x.AlternativeBarcodesCsv.Contains(normalizedSearch)));
        }

        query = ApplySort(query, sortBy, sortDir);

        return await query
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> SuggestAsync(
        string search,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var normalizedSearch = NormalizeText(search);
        if (string.IsNullOrWhiteSpace(normalizedSearch))
        {
            return [];
        }

        var safeLimit = limit <= 0 ? 8 : Math.Min(limit, 20);

        return await _dbContext.Products
            .AsNoTracking()
            .Where(x =>
                x.Code.Contains(normalizedSearch)
                || x.Name.Contains(normalizedSearch)
                || (x.ShortDescription != null && x.ShortDescription.Contains(normalizedSearch))
                || (x.Brand != null && x.Brand.Contains(normalizedSearch))
                || (x.SubCategory != null && x.SubCategory.Contains(normalizedSearch))
                || (x.BarcodeEan13 != null && x.BarcodeEan13.Contains(normalizedSearch))
                || (x.AlternativeBarcodesCsv != null && x.AlternativeBarcodesCsv.Contains(normalizedSearch))
                || (x.QrCode != null && x.QrCode.Contains(normalizedSearch)))
            .OrderByDescending(x => x.Code.StartsWith(normalizedSearch))
            .ThenByDescending(x => x.Name.StartsWith(normalizedSearch))
            .ThenBy(x => x.Code)
            .Take(safeLimit)
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<Product> ApplySort(IQueryable<Product> query, string? sortBy, string sortDir)
    {
        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        var key = sortBy?.Trim().ToLowerInvariant();

        return key switch
        {
            "name" => desc ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            "category" => desc ? query.OrderByDescending(x => x.Category) : query.OrderBy(x => x.Category),
            "brand" => desc ? query.OrderByDescending(x => x.Brand) : query.OrderBy(x => x.Brand),
            "type" or "producttype" => desc ? query.OrderByDescending(x => x.ProductType) : query.OrderBy(x => x.ProductType),
            "price" or "defaultsaleprice" => desc ? query.OrderByDescending(x => x.DefaultSalePrice) : query.OrderBy(x => x.DefaultSalePrice),
            _ => desc ? query.OrderByDescending(x => x.Code) : query.OrderBy(x => x.Code)
        };
    }

    private static string? NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
