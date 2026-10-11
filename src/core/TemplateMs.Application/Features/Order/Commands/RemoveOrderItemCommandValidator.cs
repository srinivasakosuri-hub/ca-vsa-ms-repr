using FluentValidation;

namespace TemplateMs.Application.Features.Order.Commands;

/// <summary>FluentValidation validator for <see cref="RemoveOrderItemCommand"/>.</summary>
public sealed class RemoveOrderItemCommandValidator : AbstractValidator<RemoveOrderItemCommand>
{
    /// <summary>Initializes and configures all validation rules.</summary>
    public RemoveOrderItemCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("OrderId is required.");
        RuleFor(x => x.OrderItemId).NotEmpty().WithMessage("OrderItemId is required.");
    }
}
