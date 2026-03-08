using FluentValidation;

namespace ERP.Application.Features.CariAccounts.Commands.ImportCariAccounts;

public sealed class ImportCariAccountsCommandValidator : AbstractValidator<ImportCariAccountsCommand>
{
    public ImportCariAccountsCommandValidator()
    {
        RuleFor(x => x.FileContent)
            .NotNull()
            .Must(x => x.Length > 0)
            .WithMessage("Excel file content cannot be empty.");
    }
}
