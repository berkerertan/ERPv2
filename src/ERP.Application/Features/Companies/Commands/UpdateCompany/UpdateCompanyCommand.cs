using MediatR;

namespace ERP.Application.Features.Companies.Commands.UpdateCompany;

public sealed record UpdateCompanyCommand(Guid CompanyId, string Code, string Name, string TaxNumber) : IRequest;
