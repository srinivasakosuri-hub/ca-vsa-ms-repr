using CsvHelper;
using CsvHelper.Configuration;
using FastEndpoints;
using System.Globalization;
using TemplateMs.Api.ResponseModels;
using TemplateMs.Application.Features.Order.Dtos;
using TemplateMs.Application.Features.Order.Queries;
using Wolverine;

namespace TemplateMs.Api.Endpoints.Orders;

/// <summary>
/// Request body for the order-summary CSV export.
/// Page and page-size are absent — the export always returns all matching rows.
/// </summary>
public sealed class ExportOrderSummaryCsvRequest
{
    /// <summary>Gets the column name to sort by (optional).</summary>
    public string? SortColumn    { get; init; }
    /// <summary>Gets the sort direction ('asc' or 'desc', optional).</summary>
    public string? SortDirection { get; init; }
    /// <summary>Gets the free-text search term to filter results (optional).</summary>
    public string? SearchTerm    { get; init; }
}

/// <summary>
/// Exports the order-summary list as a CSV file, applying the same sort and
/// free-text search as the grid endpoint. Every matching row is included.
/// </summary>
/// <remarks>POST api/orders/summary/export</remarks>
public sealed class ExportOrderSummaryCsvEndpoint : Endpoint<ExportOrderSummaryCsvRequest>
{
    private readonly IMessageBus _bus;

    /// <summary>Initializes a new instance of the <see cref="ExportOrderSummaryCsvEndpoint"/> class.</summary>
    /// <param name="bus">The message bus for dispatching queries.</param>
    public ExportOrderSummaryCsvEndpoint(IMessageBus bus) => _bus = bus;

    /// <summary>Configures the endpoint route and behavior.</summary>
    public override void Configure()
    {
        Post("api/orders/summary/export");
        AllowAnonymous();
        Description(b => b
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .ProducesProblem(StatusCodes.Status400BadRequest));
    }

    /// <summary>Handles the request to export order summaries as CSV.</summary>
    /// <param name="req">The request containing filter and sort parameters.</param>
    /// <param name="ct">The cancellation token.</param>
    public override async Task HandleAsync(ExportOrderSummaryCsvRequest req, CancellationToken ct)
    {
        var query = new ExportOrderSummaryQuery(
            req.SortColumn,
            req.SortDirection,
            req.SearchTerm);

        var rows = await _bus.InvokeAsync<IReadOnlyList<OrderSummaryDto>>(query, ct);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true };

        var stream = new MemoryStream();
        await using (var writer = new StreamWriter(stream, leaveOpen: true))
        await using (var csv = new CsvWriter(writer, config))
        {
            csv.WriteHeader<OrderSummaryItemResponse>();
            await csv.NextRecordAsync();

            foreach (var dto in rows!)
            {
                csv.WriteRecord(OrderSummaryItemResponse.FromDto(dto));
                await csv.NextRecordAsync();
            }
        }

        stream.Position = 0;
        var fileName = $"order-summary-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
        HttpContext.Response.StatusCode = StatusCodes.Status200OK;
        HttpContext.Response.ContentType = "text/csv";
        HttpContext.Response.Headers["Content-Disposition"] = $"attachment; filename=\"{fileName}\"";
        await stream.CopyToAsync(HttpContext.Response.Body, cancellationToken: ct);
    }
}
