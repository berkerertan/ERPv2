namespace ERP.API.Contracts.Branches;

public sealed record UpdateBranchRequest(Guid CompanyId, string Code, string Name);
