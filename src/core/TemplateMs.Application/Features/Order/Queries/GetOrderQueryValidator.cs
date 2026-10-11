using FluentValidation;

namespace TemplateMs.Application.Features.Order.Queries;

/// <summary>FluentValidation validator for <see cref="GetOrderQuery"/>.</summary>
public sealed class GetOrderQueryValidator : AbstractValidator<GetOrderQuery>
{
    private static class Rules
    {
        public const string OrderIdRequired = "OrderId is required.";
    }

    /// <summary>Initializes and configures all validation rules for <see cref="GetOrderQuery"/>.</summary>
    public GetOrderQueryValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage(Rules.OrderIdRequired);
    }
}
