using FluentAssertions;
using NSubstitute;
using TemplateMs.Application.Common;
using TemplateMs.Application.Features.Order.Dtos;
using TemplateMs.Application.Features.Order.Interfaces;
using TemplateMs.Application.Features.Order.Queries;
using Xunit;

namespace TemplateMs.Application.Tests;

/// <summary>
/// Unit tests for <see cref="GetOrdersPagedQueryHandler"/>.
/// The repository is substituted with NSubstitute; a deterministic 500-row
/// in-memory dataset is used so every paging, sorting and search assertion can be
/// reasoned about without a database.
/// </summary>
public sealed class GetOrdersPagedQueryHandlerTests
{
    // ── Constants ──────────────────────────────────────────────────────────
    private const int TotalRows   = 500;
    private const int DefaultPage = 1;

    // ── Fixed page-size variants the UI supports ───────────────────────────
    public static readonly TheoryData<int> SupportedPageSizes = new() { 10, 25, 50 };

    // ── Allowed sort columns (mirrors the production whitelist) ────────────
    public static readonly TheoryData<string, string> SortColumnAndDirectionCombinations = new()
    {
        { "orderId",      "asc"  },
        { "orderId",      "desc" },
        { "customerName", "asc"  },
        { "customerName", "desc" },
        { "orderDate",    "asc"  },
        { "orderDate",    "desc" },
        { "productName",  "asc"  },
        { "productName",  "desc" },
        { "quantity",     "asc"  },
        { "quantity",     "desc" },
        { "unitPrice",    "asc"  },
        { "unitPrice",    "desc" },
        { "totalAmount",  "asc"  },
        { "totalAmount",  "desc" },
    };

    // ── Wiring ─────────────────────────────────────────────────────────────
    private readonly IOrderReadRepository        _repository = Substitute.For<IOrderReadRepository>();
    private readonly GetOrdersPagedQueryHandler  _sut;

    public GetOrdersPagedQueryHandlerTests()
    {
        _sut = new GetOrdersPagedQueryHandler(_repository);
    }

    // ── Dataset factory ────────────────────────────────────────────────────

    /// <summary>
    /// Builds a deterministic list of <see cref="OrderSummaryDto"/> rows.
    /// <list type="bullet">
    ///   <item>Rows 1–100   → customer "Acme Corp",     products "Widget A"–"Widget D"</item>
    ///   <item>Rows 101–200 → customer "Beta Ltd",      products "Gadget A"–"Gadget D"</item>
    ///   <item>Rows 201–300 → customer "Acme Corp",     products "Widget E"–"Widget H" (intentional repeat for search tests)</item>
    ///   <item>Rows 301–400 → customer "Gamma Inc",     products "Device A"–"Device D"</item>
    ///   <item>Rows 401–500 → customer "Delta Systems", products "Module A"–"Module D"</item>
    /// </list>
    /// Quantities, unit prices, order dates, and order item IDs are deterministic
    /// so sorting assertions can verify first/last element properties.
    /// </summary>
    private static IReadOnlyList<OrderSummaryDto> BuildDataset()
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

        return Enumerable.Range(1, TotalRows)
            .Select(i => new OrderSummaryDto(
                OrderId:      i,
                CustomerName: CustomerFor(i),
                OrderDate:    new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i - 1),
                OrderItemId:  i * 10,
                ProductName:  ProductFor(i),
                Quantity:     (i % 20) + 1,               // 1–20, cycles
                UnitPrice:    Math.Round(1.00m + (i % 50) * 0.99m, 2), // 1.00–50.91
                TotalAmount:  Math.Round(((i % 20) + 1) * (1.00m + (i % 50) * 0.99m), 2)))
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Simulates the repository's paging, sorting and search logic in-memory,
    /// mirroring the SQL behaviour so handler tests can verify the returned
    /// <see cref="PagedResult{T}"/> envelope values are propagated correctly.
    /// </summary>
    private static PagedResult<OrderSummaryDto> SimulatePage(
        IReadOnlyList<OrderSummaryDto> dataset,
        int    page,
        int    pageSize,
        string? sortColumn    = null,
        string? sortDirection = null,
        string? searchTerm    = null)
    {
        IEnumerable<OrderSummaryDto> query = dataset;

        // Filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lower = searchTerm.Trim().ToLowerInvariant();
            query = query.Where(r =>
                r.CustomerName.Contains(lower, StringComparison.OrdinalIgnoreCase) ||
                r.ProductName.Contains(lower,  StringComparison.OrdinalIgnoreCase) ||
                r.OrderDate.ToString("yyyy-MM-dd").Contains(lower, StringComparison.OrdinalIgnoreCase));
        }

        // Sort — production default is DESC when direction is null/unrecognised
        var asc = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        query = sortColumn?.ToLowerInvariant() switch
        {
            "orderid"      => asc ? query.OrderBy(r => r.OrderId)      : query.OrderByDescending(r => r.OrderId),
            "customername" => asc ? query.OrderBy(r => r.CustomerName) : query.OrderByDescending(r => r.CustomerName),
            "orderdate"    => asc ? query.OrderBy(r => r.OrderDate)    : query.OrderByDescending(r => r.OrderDate),
            "productname"  => asc ? query.OrderBy(r => r.ProductName)  : query.OrderByDescending(r => r.ProductName),
            "quantity"     => asc ? query.OrderBy(r => r.Quantity)     : query.OrderByDescending(r => r.Quantity),
            "unitprice"    => asc ? query.OrderBy(r => r.UnitPrice)    : query.OrderByDescending(r => r.UnitPrice),
            "totalamount"  => asc ? query.OrderBy(r => r.TotalAmount)  : query.OrderByDescending(r => r.TotalAmount),
            _              => asc ? query.OrderBy(r => r.OrderDate)    : query.OrderByDescending(r => r.OrderDate),
        };

        var list       = query.ToList();
        var totalCount = list.Count;
        var items      = list.Skip((page - 1) * pageSize).Take(pageSize).ToList().AsReadOnly();

        return new PagedResult<OrderSummaryDto>(items, totalCount, page, pageSize);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private void SetupRepository(PagedResult<OrderSummaryDto> result,
        int page, int pageSize, string? sortColumn, string? sortDirection, string? searchTerm)
    {
        _repository
            .GetPagedSummaryAsync(page, pageSize, sortColumn, sortDirection, searchTerm, Arg.Any<CancellationToken>())
            .Returns(result);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Paging tests
    // ══════════════════════════════════════════════════════════════════════

    [Theory]
    [MemberData(nameof(SupportedPageSizes))]
    public async Task HandleWithNoFilterReturnsCorrectPageSize(int pageSize)
    {
        var dataset = BuildDataset();
        var expected = SimulatePage(dataset, DefaultPage, pageSize);
        SetupRepository(expected, DefaultPage, pageSize, null, null, null);

        var query  = new GetOrdersPagedQuery(DefaultPage, pageSize, null, null, null);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Items.Count.Should().Be(pageSize);
        result.PageSize.Should().Be(pageSize);
        result.Page.Should().Be(DefaultPage);
    }

    [Theory]
    [MemberData(nameof(SupportedPageSizes))]
    public async Task HandleWithNoFilterReturnsTotalCountEqualToDatasetSize(int pageSize)
    {
        var dataset  = BuildDataset();
        var expected = SimulatePage(dataset, DefaultPage, pageSize);
        SetupRepository(expected, DefaultPage, pageSize, null, null, null);

        var query  = new GetOrdersPagedQuery(DefaultPage, pageSize, null, null, null);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(TotalRows);
    }

    [Fact]
    public async Task HandlePage1Size10ReturnsFirst10Rows()
    {
        var dataset  = BuildDataset();
        var expected = SimulatePage(dataset, 1, 10);
        SetupRepository(expected, 1, 10, null, null, null);

        var result = await _sut.Handle(new GetOrdersPagedQuery(1, 10, null, null, null), CancellationToken.None);

        result.Items.Should().HaveCount(10);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task HandleLastPageReturnsRemainingRows()
    {
        // 500 rows / 25 per page = 20 pages exactly; last page has 25 rows
        var dataset  = BuildDataset();
        var expected = SimulatePage(dataset, 20, 25);
        SetupRepository(expected, 20, 25, null, null, null);

        var result = await _sut.Handle(new GetOrdersPagedQuery(20, 25, null, null, null), CancellationToken.None);

        result.Items.Should().HaveCount(25);
        result.Page.Should().Be(20);
        result.TotalCount.Should().Be(TotalRows);
    }

    [Fact]
    public async Task HandleTotalPagesCalculatedCorrectlyForPageSize25()
    {
        var dataset  = BuildDataset();
        var expected = SimulatePage(dataset, 1, 25);
        SetupRepository(expected, 1, 25, null, null, null);

        var result = await _sut.Handle(new GetOrdersPagedQuery(1, 25, null, null, null), CancellationToken.None);

        result.TotalPages.Should().Be(20);
    }

    [Fact]
    public async Task HandleTotalPagesCalculatedCorrectlyForPageSize50()
    {
        var dataset  = BuildDataset();
        var expected = SimulatePage(dataset, 1, 50);
        SetupRepository(expected, 1, 50, null, null, null);

        var result = await _sut.Handle(new GetOrdersPagedQuery(1, 50, null, null, null), CancellationToken.None);

        result.TotalPages.Should().Be(10);
    }

    [Fact]
    public async Task HandleDifferentPagesReturnDifferentItems()
    {
        var dataset = BuildDataset();
        var page1   = SimulatePage(dataset, 1, 10);
        var page2   = SimulatePage(dataset, 2, 10);

        SetupRepository(page1, 1, 10, null, null, null);
        SetupRepository(page2, 2, 10, null, null, null);

        var result1 = await _sut.Handle(new GetOrdersPagedQuery(1, 10, null, null, null), CancellationToken.None);
        var result2 = await _sut.Handle(new GetOrdersPagedQuery(2, 10, null, null, null), CancellationToken.None);

        result1.Items.Select(r => r.OrderId)
               .Should().NotIntersectWith(result2.Items.Select(r => r.OrderId));
    }

    // ══════════════════════════════════════════════════════════════════════
    // Sorting tests
    // ══════════════════════════════════════════════════════════════════════

    [Theory]
    [MemberData(nameof(SortColumnAndDirectionCombinations))]
    public async Task HandleWithEveryAllowedSortColumnAndDirectionReturnsPagedResult(
        string sortColumn, string sortDirection)
    {
        var dataset  = BuildDataset();
        var expected = SimulatePage(dataset, 1, 10, sortColumn, sortDirection);
        SetupRepository(expected, 1, 10, sortColumn, sortDirection, null);

        var query  = new GetOrdersPagedQuery(1, 10, sortColumn, sortDirection, null);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(10);
    }

    [Fact]
    public async Task HandleSortByCustomerNameAscFirstItemIsAlphabeticallyFirst()
    {
        var dataset  = BuildDataset();
        var expected = SimulatePage(dataset, 1, 10, "customerName", "asc");
        SetupRepository(expected, 1, 10, "customerName", "asc", null);

        var result = await _sut.Handle(new GetOrdersPagedQuery(1, 10, "customerName", "asc", null), CancellationToken.None);

        var expectedFirst = dataset.OrderBy(r => r.CustomerName).First().CustomerName;
        result.Items[0].CustomerName.Should().Be(expectedFirst);
    }

    [Fact]
    public async Task HandleSortByCustomerNameDescFirstItemIsAlphabeticallyLast()
    {
        var dataset  = BuildDataset();
        var expected = SimulatePage(dataset, 1, 10, "customerName", "desc");
        SetupRepository(expected, 1, 10, "customerName", "desc", null);

        var result = await _sut.Handle(new GetOrdersPagedQuery(1, 10, "customerName", "desc", null), CancellationToken.None);

        var expectedFirst = dataset.OrderByDescending(r => r.CustomerName).First().CustomerName;
        result.Items[0].CustomerName.Should().Be(expectedFirst);
    }

    [Fact]
    public async Task HandleSortByOrderDateAscFirstItemHasEarliestDate()
    {
        var dataset  = BuildDataset();
        var expected = SimulatePage(dataset, 1, 10, "orderDate", "asc");
        SetupRepository(expected, 1, 10, "orderDate", "asc", null);

        var result = await _sut.Handle(new GetOrdersPagedQuery(1, 10, "orderDate", "asc", null), CancellationToken.None);

        var expectedDate = dataset.Min(r => r.OrderDate);
        result.Items[0].OrderDate.Should().Be(expectedDate);
    }

    [Fact]
    public async Task HandleSortByOrderDateDescFirstItemHasLatestDate()
    {
        var dataset  = BuildDataset();
        var expected = SimulatePage(dataset, 1, 10, "orderDate", "desc");
        SetupRepository(expected, 1, 10, "orderDate", "desc", null);

        var result = await _sut.Handle(new GetOrdersPagedQuery(1, 10, "orderDate", "desc", null), CancellationToken.None);

        var expectedDate = dataset.Max(r => r.OrderDate);
        result.Items[0].OrderDate.Should().Be(expectedDate);
    }

    [Fact]
    public async Task HandleSortByQuantityAscFirstItemHasSmallestQuantity()
    {
        var dataset  = BuildDataset();
        var expected = SimulatePage(dataset, 1, 10, "quantity", "asc");
        SetupRepository(expected, 1, 10, "quantity", "asc", null);

        var result = await _sut.Handle(new GetOrdersPagedQuery(1, 10, "quantity", "asc", null), CancellationToken.None);

        var minQty = dataset.Min(r => r.Quantity);
        result.Items[0].Quantity.Should().Be(minQty);
    }

    [Fact]
    public async Task HandleSortByQuantityDescFirstItemHasLargestQuantity()
    {
        var dataset  = BuildDataset();
        var expected = SimulatePage(dataset, 1, 10, "quantity", "desc");
        SetupRepository(expected, 1, 10, "quantity", "desc", null);

        var result = await _sut.Handle(new GetOrdersPagedQuery(1, 10, "quantity", "desc", null), CancellationToken.None);

        var maxQty = dataset.Max(r => r.Quantity);
        result.Items[0].Quantity.Should().Be(maxQty);
    }

    [Fact]
    public async Task HandleSortByUnitPriceAscFirstItemHasLowestPrice()
    {
        var dataset  = BuildDataset();
        var expected = SimulatePage(dataset, 1, 10, "unitPrice", "asc");
        SetupRepository(expected, 1, 10, "unitPrice", "asc", null);

        var result = await _sut.Handle(new GetOrdersPagedQuery(1, 10, "unitPrice", "asc", null), CancellationToken.None);

        var minPrice = dataset.Min(r => r.UnitPrice);
        result.Items[0].UnitPrice.Should().Be(minPrice);
    }

    [Fact]
    public async Task HandleSortByTotalAmountDescFirstItemHasHighestTotal()
    {
        var dataset  = BuildDataset();
        var expected = SimulatePage(dataset, 1, 10, "totalAmount", "desc");
        SetupRepository(expected, 1, 10, "totalAmount", "desc", null);

        var result = await _sut.Handle(new GetOrdersPagedQuery(1, 10, "totalAmount", "desc", null), CancellationToken.None);

        var maxTotal = dataset.Max(r => r.TotalAmount);
        result.Items[0].TotalAmount.Should().Be(maxTotal);
    }

    [Fact]
    public async Task HandleSortByProductNameAscReturnsItemsInAlphaOrder()
    {
        var dataset  = BuildDataset();
        var expected = SimulatePage(dataset, 1, 50, "productName", "asc");
        SetupRepository(expected, 1, 50, "productName", "asc", null);

        var result = await _sut.Handle(new GetOrdersPagedQuery(1, 50, "productName", "asc", null), CancellationToken.None);

        var names = result.Items.Select(r => r.ProductName).ToList();
        names.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task HandleSortByOrderIdDescFirstPageContainsHighestIds()
    {
        var dataset  = BuildDataset();
        var expected = SimulatePage(dataset, 1, 10, "orderId", "desc");
        SetupRepository(expected, 1, 10, "orderId", "desc", null);

        var result = await _sut.Handle(new GetOrdersPagedQuery(1, 10, "orderId", "desc", null), CancellationToken.None);

        result.Items[0].OrderId.Should().Be(TotalRows);
    }

    [Fact]
    public async Task HandleNullSortColumnDefaultsToOrderDateDesc()
    {
        // Null sortColumn → repository receives null, defaults to OrderDate DESC
        var dataset  = BuildDataset();
        var expected = SimulatePage(dataset, 1, 10, null, null);
        SetupRepository(expected, 1, 10, null, null, null);

        var result = await _sut.Handle(new GetOrdersPagedQuery(1, 10, null, null, null), CancellationToken.None);

        // Latest date should appear first
        var latestDate = dataset.Max(r => r.OrderDate);
        result.Items[0].OrderDate.Should().Be(latestDate);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Search tests
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task HandleSearchByExactCustomerNameReturnsOnlyMatchingRows()
    {
        const string searchTerm = "Beta Ltd";
        var dataset  = BuildDataset();
        var expected = SimulatePage(dataset, 1, 25, null, null, searchTerm);
        SetupRepository(expected, 1, 25, null, null, searchTerm);

        var result = await _sut.Handle(new GetOrdersPagedQuery(1, 25, null, null, searchTerm), CancellationToken.None);

        result.Items.Should().AllSatisfy(r =>
            r.CustomerName.Should().Be("Beta Ltd"));
    }

    [Fact]
    public async Task HandleSearchByExactCustomerNameReturnsTotalCountMatchingFilter()
    {
        const string searchTerm = "Beta Ltd";
        var dataset  = BuildDataset();
        var expected = SimulatePage(dataset, 1, 25, null, null, searchTerm);
        SetupRepository(expected, 1, 25, null, null, searchTerm);

        var result = await _sut.Handle(new GetOrdersPagedQuery(1, 25, null, null, searchTerm), CancellationToken.None);

        // Dataset has rows 101–200 as Beta Ltd = 100 rows
        result.TotalCount.Should().Be(100);
    }

    [Fact]
    public async Task HandleSearchByPartialCustomerNameReturnsMatchingRows()
    {
        // "Acme" matches "Acme Corp" (rows 1–100 and 201–300 = 200 rows)
        const string searchTerm = "Acme";
        var dataset  = BuildDataset();
        var expected = SimulatePage(dataset, 1, 25, null, null, searchTerm);
        SetupRepository(expected, 1, 25, null, null, searchTerm);

        var result = await _sut.Handle(new GetOrdersPagedQuery(1, 25, null, null, searchTerm), CancellationToken.None);

        result.TotalCount.Should().Be(200);
        result.Items.Should().AllSatisfy(r =>
            r.CustomerName.Should().Contain("Acme", Exactly.Once()));
    }

    [Fact]
    public async Task HandleSearchByProductNameReturnsOnlyRowsWithMatchingProduct()
    {
        const string searchTerm = "Widget";
        var dataset  = BuildDataset();
        var expected = SimulatePage(dataset, 1, 50, null, null, searchTerm);
        SetupRepository(expected, 1, 50, null, null, searchTerm);

        var result = await _sut.Handle(new GetOrdersPagedQuery(1, 50, null, null, searchTerm), CancellationToken.None);

        // Rows 1–100 (Widget A–D) + 201–300 (Widget E–H) = 200 rows total
        result.TotalCount.Should().Be(200);
        result.Items.Should().AllSatisfy(r =>
            r.ProductName.Should().StartWith("Widget"));
    }

    [Fact]
    public async Task HandleSearchByProductNameIsCaseInsensitive()
    {
        const string lowerTerm  = "widget";
        const string upperTerm  = "WIDGET";
        var dataset  = BuildDataset();

        var lowerExpected = SimulatePage(dataset, 1, 10, null, null, lowerTerm);
        var upperExpected = SimulatePage(dataset, 1, 10, null, null, upperTerm);

        SetupRepository(lowerExpected, 1, 10, null, null, lowerTerm);
        SetupRepository(upperExpected, 1, 10, null, null, upperTerm);

        var lowerResult = await _sut.Handle(new GetOrdersPagedQuery(1, 10, null, null, lowerTerm), CancellationToken.None);
        var upperResult = await _sut.Handle(new GetOrdersPagedQuery(1, 10, null, null, upperTerm), CancellationToken.None);

        lowerResult.TotalCount.Should().Be(upperResult.TotalCount);
    }

    [Fact]
    public async Task HandleSearchByOrderDateStringReturnsMatchingRows()
    {
        // Row 1 has OrderDate 2024-01-01 → search "2024-01-01" should match it
        const string searchTerm = "2024-01-01";
        var dataset  = BuildDataset();
        var expected = SimulatePage(dataset, 1, 10, null, null, searchTerm);
        SetupRepository(expected, 1, 10, null, null, searchTerm);

        var result = await _sut.Handle(new GetOrdersPagedQuery(1, 10, null, null, searchTerm), CancellationToken.None);

        result.Items.Should().AllSatisfy(r =>
            r.OrderDate.ToString("yyyy-MM-dd").Should().Contain("2024-01-01"));
    }

    [Fact]
    public async Task HandleSearchWithNoMatchesReturnsEmptyItemsAndZeroTotalCount()
    {
        const string searchTerm = "XYZ_NO_MATCH_9999";
        var emptyResult = new PagedResult<OrderSummaryDto>(
            Array.Empty<OrderSummaryDto>().ToList().AsReadOnly(), 0, 1, 10);
        SetupRepository(emptyResult, 1, 10, null, null, searchTerm);

        var result = await _sut.Handle(new GetOrdersPagedQuery(1, 10, null, null, searchTerm), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task HandleSearchWithNullTermReturnsAllRows()
    {
        var dataset  = BuildDataset();
        var expected = SimulatePage(dataset, 1, 10, null, null, null);
        SetupRepository(expected, 1, 10, null, null, null);

        var result = await _sut.Handle(new GetOrdersPagedQuery(1, 10, null, null, null), CancellationToken.None);

        result.TotalCount.Should().Be(TotalRows);
    }

    [Fact]
    public async Task HandleSearchWithWhitespaceTermTreatedAsNoFilter()
    {
        const string whitespace = "   ";
        var dataset  = BuildDataset();
        // Whitespace is normalised to null by the repository; simulate that here
        var expected = SimulatePage(dataset, 1, 10, null, null, null);
        SetupRepository(expected, 1, 10, null, null, whitespace);

        var result = await _sut.Handle(new GetOrdersPagedQuery(1, 10, null, null, whitespace), CancellationToken.None);

        result.TotalCount.Should().Be(TotalRows);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Search + sort combined
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task HandleSearchAndSortCombinedFiltersAndSortsCorrectly()
    {
        const string searchTerm   = "Gamma Inc";
        const string sortColumn   = "orderDate";
        const string sortDirection = "asc";

        var dataset  = BuildDataset();
        var expected = SimulatePage(dataset, 1, 25, sortColumn, sortDirection, searchTerm);
        SetupRepository(expected, 1, 25, sortColumn, sortDirection, searchTerm);

        var result = await _sut.Handle(
            new GetOrdersPagedQuery(1, 25, sortColumn, sortDirection, searchTerm),
            CancellationToken.None);

        // All rows belong to Gamma Inc
        result.Items.Should().AllSatisfy(r => r.CustomerName.Should().Be("Gamma Inc"));

        // Rows sorted ascending by OrderDate
        var dates = result.Items.Select(r => r.OrderDate).ToList();
        dates.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task HandleSearchAndSortByTotalAmountDescReturnsHighestTotalsFirst()
    {
        const string searchTerm    = "Gadget";
        const string sortColumn    = "totalAmount";
        const string sortDirection = "desc";

        var dataset  = BuildDataset();
        var expected = SimulatePage(dataset, 1, 25, sortColumn, sortDirection, searchTerm);
        SetupRepository(expected, 1, 25, sortColumn, sortDirection, searchTerm);

        var result = await _sut.Handle(
            new GetOrdersPagedQuery(1, 25, sortColumn, sortDirection, searchTerm),
            CancellationToken.None);

        result.Items.Should().AllSatisfy(r => r.ProductName.Should().StartWith("Gadget"));
        var totals = result.Items.Select(r => r.TotalAmount).ToList();
        totals.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task HandleSearchAndSortAcrossMultiplePagesIsConsistent()
    {
        const string searchTerm = "Delta";
        var dataset  = BuildDataset();

        var page1 = SimulatePage(dataset, 1, 10, "orderId", "asc", searchTerm);
        var page2 = SimulatePage(dataset, 2, 10, "orderId", "asc", searchTerm);

        SetupRepository(page1, 1, 10, "orderId", "asc", searchTerm);
        SetupRepository(page2, 2, 10, "orderId", "asc", searchTerm);

        var result1 = await _sut.Handle(new GetOrdersPagedQuery(1, 10, "orderId", "asc", searchTerm), CancellationToken.None);
        var result2 = await _sut.Handle(new GetOrdersPagedQuery(2, 10, "orderId", "asc", searchTerm), CancellationToken.None);

        // Both pages should report the same total
        result1.TotalCount.Should().Be(result2.TotalCount);

        // Page 2 IDs should all be greater than page 1 IDs when sorted asc
        result2.Items.Select(r => r.OrderId).Min()
               .Should().BeGreaterThan(result1.Items.Select(r => r.OrderId).Max());
    }

    // ══════════════════════════════════════════════════════════════════════
    // Repository delegation tests
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task HandleDelegatesExactParametersToRepository()
    {
        const int    page          = 3;
        const int    pageSize      = 25;
        const string sortColumn    = "productName";
        const string sortDirection = "asc";
        const string searchTerm    = "Widget";

        var dataset  = BuildDataset();
        var expected = SimulatePage(dataset, page, pageSize, sortColumn, sortDirection, searchTerm);
        SetupRepository(expected, page, pageSize, sortColumn, sortDirection, searchTerm);

        await _sut.Handle(
            new GetOrdersPagedQuery(page, pageSize, sortColumn, sortDirection, searchTerm),
            CancellationToken.None);

        await _repository.Received(1).GetPagedSummaryAsync(
            page, pageSize, sortColumn, sortDirection, searchTerm, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandlePassesCancellationTokenToRepository()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var empty = new PagedResult<OrderSummaryDto>(
            Array.Empty<OrderSummaryDto>().ToList().AsReadOnly(), 0, 1, 10);

        _repository
            .GetPagedSummaryAsync(Arg.Any<int>(), Arg.Any<int>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), token)
            .Returns(empty);

        await _sut.Handle(new GetOrdersPagedQuery(1, 10, null, null, null), token);

        await _repository.Received(1).GetPagedSummaryAsync(
            Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), token);
    }

    [Fact]
    public async Task HandleReturnsExactPagedResultFromRepository()
    {
        var customItems = new List<OrderSummaryDto>
        {
            new(7, "Custom Corp", new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                70, "Custom Widget", 3, 9.99m, 29.97m),
        }.AsReadOnly();

        var stubResult = new PagedResult<OrderSummaryDto>(customItems, 1, 1, 10);
        SetupRepository(stubResult, 1, 10, null, null, null);

        var result = await _sut.Handle(new GetOrdersPagedQuery(1, 10, null, null, null), CancellationToken.None);

        result.Should().BeEquivalentTo(stubResult);
    }
}
