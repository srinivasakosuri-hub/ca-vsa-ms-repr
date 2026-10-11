using FluentValidation;

namespace TemplateMs.Application.Features.Order.Queries;

/// <summary>FluentValidation validator for <see cref="GetOrdersPagedQuery"/>.</summary>
public sealed class GetOrdersPagedQueryValidator : AbstractValidator<GetOrdersPagedQuery>
{
    /// <summary>Allowed sort column names (case-insensitive).</summary>
    public static readonly IReadOnlySet<string> AllowedSortColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "orderId", "customerName", "orderDate", "productName", "quantity", "unitPrice", "totalAmount"
    };

    /// <summary>Maximum length permitted for a search term.</summary>
    public const int MaxSearchTermLength = 200;

    private static class Rules
    {
        public const string PageRequired         = "Page must be greater than zero.";
        public const string PageSizeRequired     = "PageSize must be greater than zero.";
        public const string SortColumnInvalid    = "SortColumn must be one of: orderId, customerName, orderDate, productName, quantity, unitPrice, totalAmount.";
        public const string SortDirectionInvalid = "SortDirection must be 'asc' or 'desc'.";
        public static readonly string SearchTermTooLong = $"SearchTerm must not exceed {MaxSearchTermLength} characters.";
    }

    /// <summary>Initializes and configures all validation rules for <see cref="GetOrdersPagedQuery"/>.</summary>
    public GetOrdersPagedQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage(Rules.PageRequired);

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage(Rules.PageSizeRequired);

        RuleFor(x => x.SortColumn)
            .Must(col => col is null || AllowedSortColumns.Contains(col))
            .WithMessage(Rules.SortColumnInvalid);

        RuleFor(x => x.SortDirection)
            .Must(dir => dir is null ||
                         string.Equals(dir, "asc",  StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase))
            .WithMessage(Rules.SortDirectionInvalid);

        RuleFor(x => x.SearchTerm)
            .MaximumLength(MaxSearchTermLength).WithMessage(Rules.SearchTermTooLong);
    }
}
