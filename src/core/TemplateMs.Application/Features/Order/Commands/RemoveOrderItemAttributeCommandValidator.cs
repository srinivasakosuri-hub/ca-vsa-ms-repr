using FluentValidation;

namespace TemplateMs.Application.Features.Order.Commands;

/// <summary>FluentValidation validator for <see cref="RemoveOrderItemAttributeCommand"/>.</summary>
public sealed class RemoveOrderItemAttributeCommandValidator : AbstractValidator<RemoveOrderItemAttributeCommand>
{
    /// <summary>Initializes and configures all validation rules.</summary>
    public RemoveOrderItemAttributeCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("OrderId is required.");
        RuleFor(x => x.OrderItemId).NotEmpty().WithMessage("OrderItemId is required.");
        RuleFor(x => x.OrderItemAttributeId).NotEmpty().WithMessage("OrderItemAttributeId is required.");
    }
}
