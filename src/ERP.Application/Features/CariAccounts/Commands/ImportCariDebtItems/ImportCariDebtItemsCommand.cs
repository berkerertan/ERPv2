using ERP.Application.Common.Models;
using MediatR;

namespace ERP.Application.Features.CariAccounts.Commands.ImportCariDebtItems;

public sealed record ImportCariDebtItemsCommand(
    Guid CariAccountId,
    byte[] FileContent,
    bool ReplaceExisting,
    CariDebtItemImportColumnMapping? ColumnMapping) : IRequest<CariDebtItemImportResult>;
