using MediatR;

namespace ERP.Application.Features.Companies.Queries.GetCompanies;

public sealed record GetCompaniesQuery : IRequest<IReadOnlyList<CompanyDto>>;
