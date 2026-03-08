using MediatR;

namespace ERP.Application.Features.Companies.Commands.DeleteCompany;

public sealed record DeleteCompanyCommand(Guid CompanyId) : IRequest;
