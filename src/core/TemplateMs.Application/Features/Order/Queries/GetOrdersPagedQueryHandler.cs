using TemplateMs.Application.Common;
using TemplateMs.Application.Features.Order.Dtos;
using TemplateMs.Application.Features.Order.Interfaces;

namespace TemplateMs.Application.Features.Order.Queries;

/// <summary>
/// WolverineFX handler for <see cref="GetOrdersPagedQuery"/>.
/// Returns a single page of order-summary rows together with the total record count.
/// </summary>
public sealed class GetOrdersPagedQueryHandler
{
    private readonly IOrderReadRepository _readRepository;

    /// <summary>Initializes a new <see cref="GetOrdersPagedQueryHandler"/>.</summary>
    public GetOrdersPagedQueryHandler(IOrderReadRepository readRepository) =>
        _readRepository = readRepository;

    /// <summary>
    /// Handles <see cref="GetOrdersPagedQuery"/>. Delegates paging, sorting and search to the repository.
    /// </summary>
    public Task<PagedResult<OrderSummaryDto>> Handle(
        GetOrdersPagedQuery query,
        CancellationToken cancellationToken) =>
        _readRepository.GetPagedSummaryAsync(
            query.Page,
            query.PageSize,
            query.SortColumn,
            query.SortDirection,
            query.SearchTerm,
            cancellationToken);
}
