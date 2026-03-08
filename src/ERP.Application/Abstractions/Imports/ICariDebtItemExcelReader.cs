using ERP.Application.Common.Models;

namespace ERP.Application.Abstractions.Imports;

public interface ICariDebtItemExcelReader
{
    Task<IReadOnlyList<CariDebtItemExcelRow>> ReadAsync(
        Stream stream,
        CariDebtItemImportColumnMapping? mapping,
        CancellationToken cancellationToken = default);
}
