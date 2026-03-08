using FluentValidation;

namespace ERP.Application.Features.Pos.Commands.CreatePosQuickSale;

public sealed class CreatePosQuickSaleCommandValidator : AbstractValidator<CreatePosQuickSaleCommand>
{
    public CreatePosQuickSaleCommandValidator()
    {
        RuleFor(x => x.CustomerCariAccountId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
        RuleFor(x => x.Note).MaximumLength(250).When(x => !string.IsNullOrWhiteSpace(x.Note));

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.Quantity).GreaterThan(0);
            item.RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0).When(x => x.UnitPrice.HasValue);
            item.RuleFor(x => x)
                .Must(x => x.ProductId.HasValue || !string.IsNullOrWhiteSpace(x.Barcode))
                .WithMessage("Each POS item must have ProductId or Barcode.");
            item.RuleFor(x => x.Barcode)
                .MaximumLength(300)
                .When(x => !string.IsNullOrWhiteSpace(x.Barcode));
        });
    }
}
