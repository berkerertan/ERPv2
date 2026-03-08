namespace ERP.API.Contracts.Companies;

public sealed record UpdateCompanyRequest(string Code, string Name, string TaxNumber);
