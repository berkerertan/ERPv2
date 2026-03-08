using ERP.Application.Features.Companies.Queries.GetCompanies;
using MediatR;

namespace ERP.Application.Features.Companies.Queries.GetCompanyById;

public sealed record GetCompanyByIdQuery(Guid CompanyId) : IRequest<CompanyDto>;
