using FluentValidation;

namespace TemplateMs.Application.Features.Order.Commands;

/// <summary>FluentValidation validator for <see cref="AddOrderItemAttributeCommand"/>.</summary>
public sealed class AddOrderItemAttributeCommandValidator : AbstractValidator<AddOrderItemAttributeCommand>
{
    /// <summary>Initializes and configures all validation rules.</summary>
    public AddOrderItemAttributeCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("OrderId is required.");
        RuleFor(x => x.OrderItemId).NotEmpty().WithMessage("OrderItemId is required.");
        RuleFor(x => x.AttributeName).NotEmpty().WithMessage("AttributeName is required.");
        RuleFor(x => x.AttributeValue).NotEmpty().WithMessage("AttributeValue is required.");
    }
}
