using MediatR;

namespace ERP.Application.Features.Companies.Commands.CreateCompany;

public sealed record CreateCompanyCommand(string Code, string Name, string TaxNumber) : IRequest<Guid>;
