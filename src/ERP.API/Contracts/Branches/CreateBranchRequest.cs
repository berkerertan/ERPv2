namespace ERP.API.Contracts.Branches;

public sealed record CreateBranchRequest(Guid CompanyId, string Code, string Name);
