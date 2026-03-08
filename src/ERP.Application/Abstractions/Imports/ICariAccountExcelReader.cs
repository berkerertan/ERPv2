using ERP.Application.Common.Models;

namespace ERP.Application.Abstractions.Imports;

public interface ICariAccountExcelReader
{
    Task<IReadOnlyList<CariAccountExcelRow>> ReadAsync(
        Stream stream,
        CariImportColumnMapping? mapping,
        CancellationToken cancellationToken = default);
}
