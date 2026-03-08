using ERP.Application.Common.Models;
using MediatR;

namespace ERP.Application.Features.CariAccounts.Commands.ImportCariAccounts;

public sealed record ImportCariAccountsCommand(byte[] FileContent, bool UpsertExisting) : IRequest<CariAccountImportResult>;
