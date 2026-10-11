using FluentValidation;

namespace TemplateMs.Application.Features.Order.Commands;

/// <summary>FluentValidation validator for <see cref="CreateOrderCommand"/>.</summary>
public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    private static class Rules
    {
        public const string CustomerNameRequired = "CustomerName is required.";
        public const string OrderDateRequired = "OrderDate is required.";
        public const string ProductNameRequired = "ProductName is required.";
        public const string QuantityRequired = "Quantity must be greater than zero.";
        public const string UnitPriceRequired = "UnitPrice cannot be negative.";
        public const string AttributeNameRequired = "AttributeName is required.";
        public const string AttributeValueRequired = "AttributeValue is required.";
    }

    /// <summary>Initializes and configures all validation rules for <see cref="CreateOrderCommand"/>.</summary>
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerName)
            .NotEmpty().WithMessage(Rules.CustomerNameRequired);

        RuleFor(x => x.OrderDate)
            .NotEmpty().WithMessage(Rules.OrderDateRequired);

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductName).NotEmpty().WithMessage(Rules.ProductNameRequired);
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage(Rules.QuantityRequired);
            item.RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0m).WithMessage(Rules.UnitPriceRequired);

            item.RuleForEach(i => i.Attributes).ChildRules(attr =>
            {
                attr.RuleFor(a => a.AttributeName).NotEmpty().WithMessage(Rules.AttributeNameRequired);
                attr.RuleFor(a => a.AttributeValue).NotEmpty().WithMessage(Rules.AttributeValueRequired);
            });
        });
    }
}
