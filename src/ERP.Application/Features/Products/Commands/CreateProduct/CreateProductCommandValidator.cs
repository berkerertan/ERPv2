using FluentValidation;

namespace ERP.Application.Features.Products.Commands.CreateProduct;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Category).MaximumLength(100);
        RuleFor(x => x.DefaultSalePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CriticalStockLevel).GreaterThanOrEqualTo(0);

        RuleFor(x => x.BarcodeEan13)
            .MaximumLength(13)
            .Matches("^[0-9]{13}$")
            .When(x => !string.IsNullOrWhiteSpace(x.BarcodeEan13));

        RuleFor(x => x.QrCode)
            .MaximumLength(300)
            .When(x => !string.IsNullOrWhiteSpace(x.QrCode));
    }
}
