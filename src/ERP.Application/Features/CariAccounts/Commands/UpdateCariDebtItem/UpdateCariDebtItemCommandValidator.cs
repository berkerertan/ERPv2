using FluentValidation;

namespace ERP.Application.Features.CariAccounts.Commands.UpdateCariDebtItem;

public sealed class UpdateCariDebtItemCommandValidator : AbstractValidator<UpdateCariDebtItemCommand>
{
    public UpdateCariDebtItemCommandValidator()
    {
        RuleFor(x => x.CariAccountId).NotEmpty();
        RuleFor(x => x.CariDebtItemId).NotEmpty();
        RuleFor(x => x.TransactionDate).NotEmpty();
        RuleFor(x => x.MaterialDescription).NotEmpty().MaximumLength(250);
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ListPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SalePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TotalAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Payment).GreaterThanOrEqualTo(0);
    }
}
