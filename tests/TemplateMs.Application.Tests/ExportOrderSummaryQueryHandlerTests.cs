using FluentAssertions;
using NSubstitute;
using TemplateMs.Application.Features.Order.Dtos;
using TemplateMs.Application.Features.Order.Interfaces;
using TemplateMs.Application.Features.Order.Queries;
using Xunit;

namespace TemplateMs.Application.Tests;

/// <summary>
/// Unit tests for <see cref="ExportOrderSummaryQueryHandler"/>.
/// The repository is substituted with NSubstitute; a deterministic in-memory
/// dataset is used to verify delegation and result pass-through behaviour.
/// </summary>
public sealed class ExportOrderSummaryQueryHandlerTests
{
    // ── Wiring ─────────────────────────────────────────────────────────────
    private readonly IOrderReadRepository           _repository = Substitute.For<IOrderReadRepository>();
    private readonly ExportOrderSummaryQueryHandler _sut;

    public ExportOrderSummaryQueryHandlerTests()
    {
        _sut = new ExportOrderSummaryQueryHandler(_repository);
    }

    // ── Dataset factory ────────────────────────────────────────────────────

    /// <summary>
    /// Builds a deterministic list of <see cref="OrderSummaryDto"/> rows
    /// using the same customer/product groupings as the paged-query tests,
    /// giving enough variety for sort/search assertions.
    /// </summary>
    private static IReadOnlyList<OrderSummaryDto> BuildDataset(int count = 500)
    {
        static string CustomerFor(int i) => i switch
        {
            <= 100 => "Acme Corp",
            <= 200 => "Beta Ltd",
            <= 300 => "Acme Corp",
            <= 400 => "Gamma Inc",
            _      => "Delta Systems",
        };

        static string ProductFor(int i) => i switch
        {
            <= 100 => $"Widget {(char)('A' + ((i - 1)   % 4))}",
            <= 200 => $"Gadget {(char)('A' + ((i - 101) % 4))}",
            <= 300 => $"Widget {(char)('E' + ((i - 201) % 4))}",
            <= 400 => $"Device {(char)('A' + ((i - 301) % 4))}",
            _      => $"Module {(char)('A' + ((i - 401) % 4))}",
        };

        return Enumerable.Range(1, count)
            .Select(i => new OrderSummaryDto(
                OrderId:      i,
                CustomerName: CustomerFor(i),
                OrderDate:    new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i - 1),
                OrderItemId:  i * 10,
                ProductName:  ProductFor(i),
                Quantity:     (i % 20) + 1,
                UnitPrice:    Math.Round(1.00m + (i % 50) * 0.99m, 2),
                TotalAmount:  Math.Round(((i % 20) + 1) * (1.00m + (i % 50) * 0.99m), 2)))
            .ToList()
            .AsReadOnly();
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private void SetupRepository(
        IReadOnlyList<OrderSummaryDto> result,
        string? sortColumn    = null,
        string? sortDirection = null,
        string? searchTerm    = null)
    {
        _repository
            .GetAllSummaryAsync(sortColumn, sortDirection, searchTerm, Arg.Any<CancellationToken>())
            .Returns(result);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Result pass-through tests
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task HandleWithNoFiltersReturnsAllRows()
    {
        var dataset = BuildDataset();
        SetupRepository(dataset);

        var result = await _sut.Handle(new ExportOrderSummaryQuery(null, null, null), CancellationToken.None);

        result.Should().HaveCount(500);
    }

    [Fact]
    public async Task HandleReturnsExactListFromRepository()
    {
        var dataset = BuildDataset(3);
        SetupRepository(dataset);

        var result = await _sut.Handle(new ExportOrderSummaryQuery(null, null, null), CancellationToken.None);

        result.Should().BeEquivalentTo(dataset);
    }

    [Fact]
    public async Task HandleWhenRepositoryReturnsEmptyListReturnsEmptyList()
    {
        var empty = Array.Empty<OrderSummaryDto>().ToList().AsReadOnly();
        SetupRepository(empty);

        var result = await _sut.Handle(new ExportOrderSummaryQuery(null, null, null), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleReturnsSingleRowWhenRepositoryReturnsSingleRow()
    {
        var single = new List<OrderSummaryDto>
        {
            new(1, "Solo Corp", new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                10, "Widget A", 5, 9.99m, 49.95m),
        }.AsReadOnly();

        SetupRepository(single);

        var result = await _sut.Handle(new ExportOrderSummaryQuery(null, null, null), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].CustomerName.Should().Be("Solo Corp");
    }

    // ══════════════════════════════════════════════════════════════════════
    // Repository delegation — sort parameters
    // ══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("orderId",      "asc")]
    [InlineData("orderId",      "desc")]
    [InlineData("customerName", "asc")]
    [InlineData("customerName", "desc")]
    [InlineData("orderDate",    "asc")]
    [InlineData("orderDate",    "desc")]
    [InlineData("productName",  "asc")]
    [InlineData("productName",  "desc")]
    [InlineData("quantity",     "asc")]
    [InlineData("quantity",     "desc")]
    [InlineData("unitPrice",    "asc")]
    [InlineData("unitPrice",    "desc")]
    [InlineData("totalAmount",  "asc")]
    [InlineData("totalAmount",  "desc")]
    public async Task HandleDelegatesExactSortColumnAndDirectionToRepository(
        string sortColumn, string sortDirection)
    {
        var dataset = BuildDataset(10);
        SetupRepository(dataset, sortColumn, sortDirection);

        await _sut.Handle(new ExportOrderSummaryQuery(sortColumn, sortDirection, null), CancellationToken.None);

        await _repository.Received(1).GetAllSummaryAsync(
            sortColumn, sortDirection, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleWithNullSortColumnDelegatesNullToRepository()
    {
        var dataset = BuildDataset(5);
        SetupRepository(dataset, null, null);

        await _sut.Handle(new ExportOrderSummaryQuery(null, null, null), CancellationToken.None);

        await _repository.Received(1).GetAllSummaryAsync(
            null, null, null, Arg.Any<CancellationToken>());
    }

    // ══════════════════════════════════════════════════════════════════════
    // Repository delegation — search term
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task HandleDelegatesSearchTermToRepository()
    {
        const string searchTerm = "Acme";
        var dataset = BuildDataset().Where(r =>
            r.CustomerName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList().AsReadOnly();

        SetupRepository(dataset, null, null, searchTerm);

        await _sut.Handle(new ExportOrderSummaryQuery(null, null, searchTerm), CancellationToken.None);

        await _repository.Received(1).GetAllSummaryAsync(
            null, null, searchTerm, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleWithNullSearchTermDelegatesNullToRepository()
    {
        var dataset = BuildDataset(5);
        SetupRepository(dataset, null, null, null);

        await _sut.Handle(new ExportOrderSummaryQuery(null, null, null), CancellationToken.None);

        await _repository.Received(1).GetAllSummaryAsync(
            null, null, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleWithSearchTermAndSortDelegatesBothToRepository()
    {
        const string searchTerm    = "Widget";
        const string sortColumn    = "productName";
        const string sortDirection = "asc";

        var dataset = BuildDataset(10);
        SetupRepository(dataset, sortColumn, sortDirection, searchTerm);

        await _sut.Handle(
            new ExportOrderSummaryQuery(sortColumn, sortDirection, searchTerm),
            CancellationToken.None);

        await _repository.Received(1).GetAllSummaryAsync(
            sortColumn, sortDirection, searchTerm, Arg.Any<CancellationToken>());
    }

    // ══════════════════════════════════════════════════════════════════════
    // Repository delegation — CancellationToken
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task HandlePassesCancellationTokenToRepository()
    {
        using var cts   = new CancellationTokenSource();
        var       token = cts.Token;

        _repository
            .GetAllSummaryAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), token)
            .Returns(Array.Empty<OrderSummaryDto>().ToList().AsReadOnly());

        await _sut.Handle(new ExportOrderSummaryQuery(null, null, null), token);

        await _repository.Received(1).GetAllSummaryAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), token);
    }

    [Fact]
    public async Task HandleCallsRepositoryExactlyOnce()
    {
        var dataset = BuildDataset(5);
        SetupRepository(dataset);

        await _sut.Handle(new ExportOrderSummaryQuery(null, null, null), CancellationToken.None);

        await _repository.Received(1).GetAllSummaryAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    // ══════════════════════════════════════════════════════════════════════
    // Search-filtered result tests (simulate repository returning filtered data)
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task HandleWhenSearchTermFilteredRepositoryReturnsOnlyMatchingRows()
    {
        const string searchTerm = "Beta Ltd";
        var filtered = BuildDataset()
            .Where(r => r.CustomerName.Equals(searchTerm, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();

        SetupRepository(filtered, null, null, searchTerm);

        var result = await _sut.Handle(
            new ExportOrderSummaryQuery(null, null, searchTerm), CancellationToken.None);

        result.Should().HaveCount(100);
        result.Should().AllSatisfy(r => r.CustomerName.Should().Be("Beta Ltd"));
    }

    [Fact]
    public async Task HandleWhenNoMatchesRepositoryReturnsEmptyAndHandlerPropagatesEmpty()
    {
        const string searchTerm = "NOMATCH_XYZ";
        var empty = Array.Empty<OrderSummaryDto>().ToList().AsReadOnly();
        SetupRepository(empty, null, null, searchTerm);

        var result = await _sut.Handle(
            new ExportOrderSummaryQuery(null, null, searchTerm), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleWhenSortedRepositoryResultOrderIsPreserved()
    {
        // Repository returns rows already sorted DESC by OrderId — handler must not re-sort
        var sorted = BuildDataset(20)
            .OrderByDescending(r => r.OrderId)
            .ToList()
            .AsReadOnly();

        SetupRepository(sorted, "orderId", "desc");

        var result = await _sut.Handle(
            new ExportOrderSummaryQuery("orderId", "desc", null), CancellationToken.None);

        result.Select(r => r.OrderId).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task HandlePreservesAllFieldsOfEachReturnedRow()
    {
        var expected = new OrderSummaryDto(
            OrderId:      42,
            CustomerName: "Verify Corp",
            OrderDate:    new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            OrderItemId:  420,
            ProductName:  "Precision Part",
            Quantity:     7,
            UnitPrice:    14.99m,
            TotalAmount:  104.93m);

        var stub = new List<OrderSummaryDto> { expected }.AsReadOnly();
        SetupRepository(stub);

        var result = await _sut.Handle(new ExportOrderSummaryQuery(null, null, null), CancellationToken.None);

        var row = result.Single();
        row.OrderId.Should().Be(42);
        row.CustomerName.Should().Be("Verify Corp");
        row.OrderDate.Should().Be(new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc));
        row.OrderItemId.Should().Be(420);
        row.ProductName.Should().Be("Precision Part");
        row.Quantity.Should().Be(7);
        row.UnitPrice.Should().Be(14.99m);
        row.TotalAmount.Should().Be(104.93m);
    }

    // ══════════════════════════════════════════════════════════════════════
    // 500-record dataset: sort column assertions (repository pre-sorts, handler preserves order)
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task HandleWith500RowsSortedByCustomerNameAscPreservesAlphaOrder()
    {
        var sorted = BuildDataset(500)
            .OrderBy(r => r.CustomerName)
            .ToList()
            .AsReadOnly();

        SetupRepository(sorted, "customerName", "asc");

        var result = await _sut.Handle(
            new ExportOrderSummaryQuery("customerName", "asc", null), CancellationToken.None);

        result.Should().HaveCount(500);
        result.Select(r => r.CustomerName).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task HandleWith500RowsSortedByOrderDateDescPreservesDescOrder()
    {
        var sorted = BuildDataset(500)
            .OrderByDescending(r => r.OrderDate)
            .ToList()
            .AsReadOnly();

        SetupRepository(sorted, "orderDate", "desc");

        var result = await _sut.Handle(
            new ExportOrderSummaryQuery("orderDate", "desc", null), CancellationToken.None);

        result.Should().HaveCount(500);
        result.Select(r => r.OrderDate).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task HandleWith500RowsFilteredByWidgetAndSortedByProductNameAscPreservesOrder()
    {
        const string searchTerm = "Widget";
        var filtered = BuildDataset(500)
            .Where(r => r.ProductName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => r.ProductName)
            .ToList()
            .AsReadOnly();

        SetupRepository(filtered, "productName", "asc", searchTerm);

        var result = await _sut.Handle(
            new ExportOrderSummaryQuery("productName", "asc", searchTerm), CancellationToken.None);

        result.Should().HaveCount(200);
        result.Should().AllSatisfy(r => r.ProductName.Should().StartWith("Widget"));
        result.Select(r => r.ProductName).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task HandleWith500RowsFilteredByGammaIncReturnsCorrectCount()
    {
        const string searchTerm = "Gamma Inc";
        var filtered = BuildDataset(500)
            .Where(r => r.CustomerName.Equals(searchTerm, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();

        SetupRepository(filtered, null, null, searchTerm);

        var result = await _sut.Handle(
            new ExportOrderSummaryQuery(null, null, searchTerm), CancellationToken.None);

        result.Should().HaveCount(100);
        result.Should().AllSatisfy(r => r.CustomerName.Should().Be("Gamma Inc"));
    }

    [Fact]
    public async Task HandleWith500RowsSortedByTotalAmountDescFirstRowHasHighestTotal()
    {
        var dataset = BuildDataset(500);
        var sorted  = dataset.OrderByDescending(r => r.TotalAmount).ToList().AsReadOnly();
        SetupRepository(sorted, "totalAmount", "desc");

        var result = await _sut.Handle(
            new ExportOrderSummaryQuery("totalAmount", "desc", null), CancellationToken.None);

        result[0].TotalAmount.Should().Be(dataset.Max(r => r.TotalAmount));
    }

    [Fact]
    public async Task HandleWith500RowsSortedByQuantityAscFirstRowHasSmallestQuantity()
    {
        var dataset = BuildDataset(500);
        var sorted  = dataset.OrderBy(r => r.Quantity).ToList().AsReadOnly();
        SetupRepository(sorted, "quantity", "asc");

        var result = await _sut.Handle(
            new ExportOrderSummaryQuery("quantity", "asc", null), CancellationToken.None);

        result[0].Quantity.Should().Be(dataset.Min(r => r.Quantity));
    }
}
