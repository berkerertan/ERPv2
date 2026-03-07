namespace ERP.Application.Features.Branches.Queries.GetBranches;

public sealed record BranchDto(Guid Id, Guid CompanyId, string Code, string Name);
