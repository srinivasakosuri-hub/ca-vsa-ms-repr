using TemplateMs.Application.Features.Order.Dtos;
using TemplateMs.Application.Features.Order.Interfaces;

namespace TemplateMs.Application.Features.Order.Queries;

/// <summary>
/// Wolverine handler for <see cref="ExportOrderSummaryQuery"/>.
/// Returns every matching <see cref="OrderSummaryDto"/> row so the
/// presentation layer can stream the result as a CSV file.
/// </summary>
public sealed class ExportOrderSummaryQueryHandler
{
    private readonly IOrderReadRepository _readRepository;

    /// <summary>Initializes a new <see cref="ExportOrderSummaryQueryHandler"/>.</summary>
    public ExportOrderSummaryQueryHandler(IOrderReadRepository readRepository) =>
        _readRepository = readRepository;

    /// <summary>Handles the export query and returns all matching rows.</summary>
    public Task<IReadOnlyList<OrderSummaryDto>> Handle(
        ExportOrderSummaryQuery query,
        CancellationToken cancellationToken) =>
        _readRepository.GetAllSummaryAsync(
            query.SortColumn,
            query.SortDirection,
            query.SearchTerm,
            cancellationToken);
}
