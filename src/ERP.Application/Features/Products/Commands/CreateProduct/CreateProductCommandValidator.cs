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
        RuleFor(x => x.ShortDescription).MaximumLength(500);
        RuleFor(x => x.SubCategory).MaximumLength(100);
        RuleFor(x => x.Brand).MaximumLength(100);
        RuleFor(x => x.ProductType).MaximumLength(50);
        RuleFor(x => x.DefaultShelfCode).MaximumLength(100);
        RuleFor(x => x.ImageUrl).MaximumLength(1000);
        RuleFor(x => x.TechnicalDocumentUrl).MaximumLength(1000);

        RuleFor(x => x.PurchaseVatRate)
            .InclusiveBetween(0, 100)
            .When(x => x.PurchaseVatRate.HasValue);

        RuleFor(x => x.SalesVatRate)
            .InclusiveBetween(0, 100)
            .When(x => x.SalesVatRate.HasValue);

        RuleFor(x => x.MinimumStockLevel)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinimumStockLevel.HasValue);

        RuleFor(x => x.DefaultSalePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CriticalStockLevel).GreaterThanOrEqualTo(0);

        RuleFor(x => x.MaximumStockLevel)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MaximumStockLevel.HasValue);

        RuleFor(x => x)
            .Must(x => !x.MaximumStockLevel.HasValue
                       || !x.MinimumStockLevel.HasValue
                       || x.MaximumStockLevel.Value >= x.MinimumStockLevel.Value)
            .WithMessage("Maximum stock level must be greater than or equal to minimum stock level.");

        RuleFor(x => x.LastPurchasePrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.LastPurchasePrice.HasValue);

        RuleFor(x => x.LastSalePrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.LastSalePrice.HasValue);

        RuleFor(x => x.AlternativeUnits)
            .Must(x => x is null || x.Count <= 30)
            .WithMessage("Alternative units cannot exceed 30 items.");

        RuleForEach(x => x.AlternativeUnits)
            .NotEmpty()
            .MaximumLength(30);

        RuleFor(x => x.AlternativeBarcodes)
            .Must(x => x is null || x.Count <= 100)
            .WithMessage("Alternative barcodes cannot exceed 100 items.");

        RuleForEach(x => x.AlternativeBarcodes)
            .NotEmpty()
            .MaximumLength(80);

        RuleFor(x => x.BarcodeEan13)
            .MaximumLength(13)
            .Matches("^[0-9]{13}$")
            .When(x => !string.IsNullOrWhiteSpace(x.BarcodeEan13));

        RuleFor(x => x.QrCode)
            .MaximumLength(300)
            .When(x => !string.IsNullOrWhiteSpace(x.QrCode));
    }
}
