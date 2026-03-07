namespace ERP.API.Contracts.Companies;

public sealed record CreateCompanyRequest(string Code, string Name, string TaxNumber);
