namespace ERP.Application.Features.Companies.Queries.GetCompanies;

public sealed record CompanyDto(Guid Id, string Code, string Name, string TaxNumber);
