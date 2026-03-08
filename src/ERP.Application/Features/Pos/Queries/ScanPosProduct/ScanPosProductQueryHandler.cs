using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using MediatR;

namespace ERP.Application.Features.Pos.Queries.ScanPosProduct;

public sealed class ScanPosProductQueryHandler(
    IProductRepository productRepository,
    IStockMovementRepository stockMovementRepository)
    : IRequestHandler<ScanPosProductQuery, PosProductScanDto>
{
    public async Task<PosProductScanDto> Handle(ScanPosProductQuery request, CancellationToken cancellationToken)
    {
        var barcode = NormalizeBarcode(request.Barcode);
        if (string.IsNullOrWhiteSpace(barcode))
        {
            throw new NotFoundException("Barcode is empty.");
        }

        var product = await productRepository.GetByBarcodeAsync(barcode, cancellationToken)
            ?? throw new NotFoundException("Product not found for scanned barcode/QR.");

        var availableStock = await stockMovementRepository.GetCurrentQuantityAsync(request.WarehouseId, product.Id, cancellationToken);

        return new PosProductScanDto(
            product.Id,
            product.Code,
            product.Name,
            product.Unit,
            product.Category,
            product.BarcodeEan13,
            product.QrCode,
            product.DefaultSalePrice,
            availableStock);
    }

    private static string NormalizeBarcode(string value)
    {
        return value.Trim().Replace("\r", string.Empty).Replace("\n", string.Empty);
    }
}
