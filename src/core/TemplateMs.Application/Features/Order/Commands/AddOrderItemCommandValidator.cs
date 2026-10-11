using FluentValidation;

namespace TemplateMs.Application.Features.Order.Commands;

/// <summary>FluentValidation validator for <see cref="AddOrderItemCommand"/>.</summary>
public sealed class AddOrderItemCommandValidator : AbstractValidator<AddOrderItemCommand>
{
    /// <summary>Initializes and configures all validation rules.</summary>
    public AddOrderItemCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("OrderId is required.");
        RuleFor(x => x.ProductName).NotEmpty().WithMessage("ProductName is required.");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than zero.");
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0m).WithMessage("UnitPrice cannot be negative.");
    }
}
