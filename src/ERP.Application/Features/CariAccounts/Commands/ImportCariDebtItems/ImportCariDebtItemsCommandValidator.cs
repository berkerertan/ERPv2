using FluentValidation;

namespace ERP.Application.Features.CariAccounts.Commands.ImportCariDebtItems;

public sealed class ImportCariDebtItemsCommandValidator : AbstractValidator<ImportCariDebtItemsCommand>
{
    public ImportCariDebtItemsCommandValidator()
    {
        RuleFor(x => x.CariAccountId).NotEmpty();
        RuleFor(x => x.FileContent)
            .NotNull()
            .Must(x => x.Length > 0)
            .WithMessage("Excel file content cannot be empty.");
    }
}
