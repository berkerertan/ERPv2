using MediatR;

namespace ERP.Application.Features.Pos.Queries.ScanPosProduct;

public sealed record ScanPosProductQuery(Guid WarehouseId, string Barcode) : IRequest<PosProductScanDto>;
