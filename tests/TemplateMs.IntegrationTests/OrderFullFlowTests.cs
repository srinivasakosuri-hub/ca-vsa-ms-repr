using CsvHelper;
using Dapper;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using TemplateMs.Api.Endpoints.Orders;
using TemplateMs.Api.ResponseModels;
using TemplateMs.Sql.Features.Order.Commands;
using Xunit;

namespace TemplateMs.IntegrationTests;

/// <summary>
/// <see cref="WebApplicationFactory{TEntryPoint}"/> subclass that wires the test host
/// to the shared Testcontainers SQL Server instance and replaces Wolverine's SQL Server
/// persistence with in-memory storage, so <c>WolverineRuntime.StopAsync</c> never
/// attempts to write to the (already-stopping) container on teardown.
/// </summary>
internal sealed class OrderTestFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public OrderTestFactory(string connectionString) =>
        _connectionString = connectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment to enable test-specific behavior
        builder.UseEnvironment("Test");

        // Point both DB connection strings at the shared container.
        builder.UseSetting("Infrastructure:CommandConnectionString", _connectionString);
        builder.UseSetting("Infrastructure:QueryConnectionString",   _connectionString);

        // Instruct WolverineRegistration.Configure to skip PersistMessagesWithSqlServer
        // so WolverineRuntime.StopAsync never executes SQL during teardown, which would
        // otherwise cause [Test Class Cleanup Failure] SqlExceptions after container disposal.
        builder.UseSetting("Infrastructure:UseInMemoryTransport", "true");

        builder.ConfigureServices(services =>
        {
            // Remove background services that require external infrastructure (AMQ).
            RemoveHostedService<Infrastructure.Messaging.Amq.ActiveMqConsumer>(services);
            RemoveHostedService<Infrastructure.Messaging.Outbox.OutboxProcessor>(services);
            RemoveHostedService<Infrastructure.Messaging.Inbox.InboxProcessor>(services);
        });
    }

    private static void RemoveHostedService<T>(IServiceCollection services)
    {
        var descriptor = services.FirstOrDefault(d => d.ImplementationType == typeof(T));
        if (descriptor is not null)
        {
            services.Remove(descriptor);
        }
    }
}

/// <summary>
/// Full HTTP-stack integration test that exercises every endpoint
/// action method in a single ordered scenario against a real SQL Server container.
/// <para>
/// Flow:
/// <list type="number">
///   <item>POST   /api/orders            — create an order</item>
///   <item>GET    /api/orders/{id}       — retrieve it by ID</item>
///   <item>POST   /api/orders/{id}/items — add a line item</item>
///   <item>POST   /api/orders/{id}/items/{itemId}/attributes — add an attribute to the item</item>
///   <item>QUERY  /api/orders/summary    — paged grid query (search + sort)</item>
///   <item>POST   /api/orders/summary/export — CSV export</item>
///   <item>DELETE /api/orders/{id}/items/{itemId}/attributes/{attrId} — remove the attribute</item>
///   <item>DELETE /api/orders/{id}/items/{itemId} — remove the item</item>
/// </list>
/// </para>
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class OrderFullFlowTests : IDisposable
{
    private readonly OrderTestFactory _factory;
    private readonly SqlServerFixture           _dbFixture;

    public OrderFullFlowTests(SqlServerFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _factory   = new OrderTestFactory(_dbFixture.ConnectionString);
    }

    public void Dispose() => _factory.Dispose();

    // ══════════════════════════════════════════════════════════════════════
    // Helpers
    // ══════════════════════════════════════════════════════════════════════

    private HttpClient CreateClient() => _factory.CreateClient();

    /// <summary>Seeds a known set of order rows directly via SQL so the summary and export
    /// endpoints have predictable data to query against.</summary>
    private async Task<List<(int OrderId, int ItemId)>> SeedSummaryDataAsync(int count = 10)
    {
        using var connection = new SqlConnection(_dbFixture.ConnectionString);
        var results = new List<(int, int)>();

        for (var i = 1; i <= count; i++)
        {
            var customerId = (i % 3) switch
            {
                0 => "Seed Corp A",
                1 => "Seed Corp B",
                _ => "Seed Corp C",
            };

            var orderId = await connection.ExecuteScalarAsync<int>(OrderCommandSql.Insert, new
            {
                CustomerName = customerId,
                OrderDate    = new DateTime(2024, 1, i, 0, 0, 0, DateTimeKind.Utc),
                IsActive     = true,
                CreatedBy    = Guid.NewGuid(),
                CreatedAt    = DateTime.UtcNow,
                UpdatedBy    = Guid.NewGuid(),
                UpdatedAt    = DateTime.UtcNow,
            });

            var itemId = await connection.ExecuteScalarAsync<int>(OrderItemCommandSql.Insert, new
            {
                OrderId     = orderId,
                ProductName = $"Seed Product {i}",
                Quantity    = i,
                UnitPrice   = i * 1.50m,
                IsActive    = true,
                CreatedBy   = Guid.NewGuid(),
                CreatedAt   = DateTime.UtcNow,
                UpdatedBy   = Guid.NewGuid(),
                UpdatedAt   = DateTime.UtcNow,
            });

            results.Add((orderId, itemId));
        }

        return results;
    }

    // ══════════════════════════════════════════════════════════════════════
    // Single ordered scenario covering all endpoints
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Exercises every endpoint action in a single
    /// end-to-end flow, using a real SQL Server container started by <see cref="SqlServerFixture"/>
    /// and a real ASP.NET Core test host started by <see cref="WebApplicationFactory{TEntryPoint}"/>.
    /// </summary>
    [Fact]
    public async Task AllEndpoints_FullFlow_SucceedWithExpectedResults()
    {
        var client = CreateClient();
        var orderDate = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc);

        // ── Step 1: POST /api/orders  ──────────────────────────────────────
        // Create a new order with one line item that itself has one attribute.
        var createRequest = new
        {
            customerName = "Integration Customer",
            orderDate    = orderDate,
            items        = new[]
            {
                new
                {
                    productName = "Integration Widget",
                    quantity    = 3,
                    unitPrice   = 25.00m,
                    attributes  = new[]
                    {
                        new { attributeName = "Colour", attributeValue = "Blue" },
                    },
                },
            },
        };

        var createResponse = await client.PostAsJsonAsync("/api/orders", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created,
            because: "a valid Create payload must return 201 Created");

        var created = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();
        created.Should().NotBeNull();
        created!.CustomerName.Should().Be("Integration Customer");
        created.IsActive.Should().BeTrue();
        created.Items.Should().HaveCount(1);

        var orderId  = created.Id;
        var itemId   = created.Items[0].Id;
        var attrId   = created.Items[0].Attributes[0].Id;

        createResponse.Headers.Location!.ToString()
            .Should().EndWith($"/api/orders/{orderId}",
                because: "Location header must point to the new resource");

        // ── Step 2: GET /api/orders/{id}  ─────────────────────────────────
        // Retrieve the order that was just created.
        var getResponse = await client.GetAsync($"/api/orders/{orderId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "a valid order ID must return 200 OK");

        var fetched = await getResponse.Content.ReadFromJsonAsync<OrderResponse>();
        fetched.Should().NotBeNull();
        fetched!.Id.Should().Be(orderId);
        fetched.CustomerName.Should().Be("Integration Customer");
        fetched.OrderDate.Should().Be(orderDate);
        fetched.IsActive.Should().BeTrue();
        fetched.Items.Should().HaveCount(1);
        fetched.Items[0].ProductName.Should().Be("Integration Widget");
        fetched.Items[0].Quantity.Should().Be(3);
        fetched.Items[0].UnitPrice.Should().Be(25.00m);
        fetched.Items[0].Attributes.Should().HaveCount(1);
        fetched.Items[0].Attributes[0].AttributeName.Should().Be("Colour");
        fetched.Items[0].Attributes[0].AttributeValue.Should().Be("Blue");

        // ── Step 3: POST /api/orders/{id}/items  ──────────────────────────
        // Add a second line item to the order.
        var addItemRequest = new
        {
            productName = "Extra Part",
            quantity    = 1,
            unitPrice   = 9.99m,
        };

        var addItemResponse = await client.PostAsJsonAsync(
            $"/api/orders/{orderId}/items", addItemRequest);
        addItemResponse.StatusCode.Should().Be(HttpStatusCode.Created,
            because: "AddItem must return 201 Created");

        var addedItem = await addItemResponse.Content.ReadFromJsonAsync<OrderItemResponse>();
        addedItem.Should().NotBeNull();
        addedItem!.ProductName.Should().Be("Extra Part");
        addedItem.Quantity.Should().Be(1);
        addedItem.UnitPrice.Should().Be(9.99m);

        var newItemId = addedItem.Id;

        // Confirm the order now has two items
        var afterAddItem = await client.GetAsync($"/api/orders/{orderId}");
        var afterAddItemBody = await afterAddItem.Content.ReadFromJsonAsync<OrderResponse>();
        afterAddItemBody!.Items.Should().HaveCount(2,
            because: "the order should have the original item plus the newly added item");

        // ── Step 4: POST /api/orders/{id}/items/{itemId}/attributes  ──────
        // Add an attribute to the new item.
        var addAttrRequest = new
        {
            attributeName  = "Weight",
            attributeValue = "500g",
        };

        var addAttrResponse = await client.PostAsJsonAsync(
            $"/api/orders/{orderId}/items/{newItemId}/attributes", addAttrRequest);
        addAttrResponse.StatusCode.Should().Be(HttpStatusCode.Created,
            because: "AddAttribute must return 201 Created");

        var addedAttr = await addAttrResponse.Content.ReadFromJsonAsync<OrderItemAttributeResponse>();
        addedAttr.Should().NotBeNull();
        addedAttr!.AttributeName.Should().Be("Weight");
        addedAttr.AttributeValue.Should().Be("500g");

        var newAttrId = addedAttr.Id;

        // Confirm the new item now has one attribute
        var afterAddAttr = await client.GetAsync($"/api/orders/{orderId}");
        var afterAddAttrBody = await afterAddAttr.Content.ReadFromJsonAsync<OrderResponse>();
        var newItemAfterAttr = afterAddAttrBody!.Items.Single(i => i.Id == newItemId);
        newItemAfterAttr.Attributes.Should().HaveCount(1,
            because: "the newly added attribute must be visible on the item");

        // ── Step 5: QUERY /api/orders/summary  ────────────────────────────
        // Seed additional rows so there is meaningful data to sort and search.
        await SeedSummaryDataAsync(10);

        // Exercise the paged summary endpoint — sort by customerName ASC, search for "Integration".
        var summaryRequest = new HttpRequestMessage(HttpMethod.Post, "/api/orders/summary")
        {
            Content = JsonContent.Create(new GetOrderSummaryPagedRequest
            {
                Page = 1,
                PageSize = 25,
                SortColumn = "customerName",
                SortDirection = "asc",
                SearchTerm = "Integration Customer"
            }),
        };

        var summaryResponse = await client.SendAsync(summaryRequest);
        summaryResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "POST /summary must return 200 OK");

        var summary = await summaryResponse.Content.ReadFromJsonAsync<OrderSummaryPagedResponse>();
        summary.Should().NotBeNull();
        summary!.TotalCount.Should().BeGreaterThanOrEqualTo(1,
            because: "at least the order we created must match the 'Integration Customer' search");
        summary.Items.Should().NotBeEmpty();
        summary.Items.Should().AllSatisfy(r =>
            r.CustomerName.Should().Contain("Integration Customer"));

        // Verify sort order: customerName ASC means items are in ascending order
        var customerNames = summary.Items.Select(r => r.CustomerName).ToList();
        customerNames.Should().BeInAscendingOrder();

        // Verify paging metadata
        summary.Page.Should().Be(1);
        summary.PageSize.Should().Be(25);
        summary.TotalPages.Should().BeGreaterThanOrEqualTo(1);

        // ── Step 6: POST /api/orders/summary/export  ──────────────────────
        // Export the same filtered rows as a CSV file.
        var exportRequest = new
        {
            sortColumn    = "customerName",
            sortDirection = "asc",
            searchTerm    = "Integration Customer",
        };

        var exportResponse = await client.PostAsJsonAsync("/api/orders/summary/export", exportRequest);
        exportResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "CSV export must return 200 OK");
        exportResponse.Content.Headers.ContentType!.MediaType.Should().Be("text/csv",
            because: "the export endpoint must produce a text/csv response");

        var contentDisposition = exportResponse.Content.Headers.ContentDisposition;
        contentDisposition.Should().NotBeNull();
        contentDisposition!.FileName.Should().MatchRegex(@"order-summary-\d{8}-\d{6}\.csv",
            because: "the filename must follow the order-summary-yyyyMMdd-HHmmss.csv pattern");

        var csvBytes  = await exportResponse.Content.ReadAsByteArrayAsync();
        var csvContent = Encoding.UTF8.GetString(csvBytes);
        csvContent.Should().NotBeNullOrWhiteSpace();

        // Parse CSV and validate it has the expected columns and at least one data row
        using var csvReader = new CsvReader(new StringReader(csvContent), CultureInfo.InvariantCulture);
        var csvRecords = csvReader.GetRecords<dynamic>().ToList();
        csvRecords.Should().NotBeEmpty(
            because: "the export must contain at least the order we created");

        // Verify header columns are present in the first record's keys
        var firstRecord = (IDictionary<string, object>)csvRecords.First();
        firstRecord.Keys.Should().Contain("OrderId");
        firstRecord.Keys.Should().Contain("CustomerName");
        firstRecord.Keys.Should().Contain("OrderDate");
        firstRecord.Keys.Should().Contain("OrderItemId");
        firstRecord.Keys.Should().Contain("ProductName");
        firstRecord.Keys.Should().Contain("Quantity");
        firstRecord.Keys.Should().Contain("UnitPrice");
        firstRecord.Keys.Should().Contain("TotalAmount");

        // All exported rows must match the search filter
        foreach (IDictionary<string, object> row in csvRecords)
        {
            row["CustomerName"].ToString().Should().Contain("Integration Customer");
        }

        // CSV row count must match the summary total count
        csvRecords.Should().HaveCount(summary.TotalCount,
            because: "the CSV export must contain exactly the same number of rows as the grid query");

        // ── Step 7: DELETE /api/orders/{id}/items/{itemId}/attributes/{attrId}
        // Remove the attribute we added in step 4 from the new item.
        var removeAttrResponse = await client.DeleteAsync(
            $"/api/orders/{orderId}/items/{newItemId}/attributes/{newAttrId}");
        removeAttrResponse.StatusCode.Should().Be(HttpStatusCode.NoContent,
            because: "RemoveAttribute must return 204 No Content");

        // Confirm the attribute is gone
        var afterRemoveAttr = await client.GetAsync($"/api/orders/{orderId}");
        var afterRemoveAttrBody = await afterRemoveAttr.Content.ReadFromJsonAsync<OrderResponse>();
        var itemAfterRemoveAttr = afterRemoveAttrBody!.Items.Single(i => i.Id == newItemId);
        itemAfterRemoveAttr.Attributes.Should().NotContain(
            a => a.IsActive,
            because: "the attribute must be soft-deleted (IsActive = false) after removal");

        // ── Step 8: DELETE /api/orders/{id}/items/{itemId}  ───────────────
        // Remove the item we added in step 3.
        var removeItemResponse = await client.DeleteAsync(
            $"/api/orders/{orderId}/items/{newItemId}");
        removeItemResponse.StatusCode.Should().Be(HttpStatusCode.NoContent,
            because: "RemoveItem must return 204 No Content");

        // Confirm the order still has 2 items but only 1 is active (the original)
        var afterRemoveItem = await client.GetAsync($"/api/orders/{orderId}");
        var afterRemoveItemBody = await afterRemoveItem.Content.ReadFromJsonAsync<OrderResponse>();
        afterRemoveItemBody!.Items.Count(i => i.IsActive).Should().Be(1,
            because: "only the original item must be active after the second item is soft-deleted");
        afterRemoveItemBody.Items.First(i => i.IsActive).Id.Should().Be(itemId,
            because: "the surviving active item must be the original one created in step 1");
    }

    // ══════════════════════════════════════════════════════════════════════
    // Error path: GET non-existent order returns 404
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetById_WithNonExistentId_Returns404()
    {
        var client   = CreateClient();
        var response = await client.GetAsync("/api/orders/999999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Paging: summary returns correct page metadata
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetSummaryPaged_WithPageSize10_ReturnsCorrectPagingMetadata()
    {
        await SeedSummaryDataAsync(10);
        var client = CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders/summary")
        {
            Content = JsonContent.Create(new GetOrderSummaryPagedRequest
            {
                Page = 1,
                PageSize = 10,
                SortColumn = "orderDate",
                SortDirection = "desc"
            }),
        };

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<OrderSummaryPagedResponse>();
        body.Should().NotBeNull();
        body!.Page.Should().Be(1);
        body.PageSize.Should().Be(10);
        body.Items.Should().HaveCountLessThanOrEqualTo(10);
        body.TotalCount.Should().BeGreaterThanOrEqualTo(1);
        body.TotalPages.Should().BeGreaterThanOrEqualTo(1);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Sort: summary returns items sorted correctly
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetSummaryPaged_SortByCustomerNameAsc_ReturnsItemsInAscendingOrder()
    {
        await SeedSummaryDataAsync(5);
        var client = CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders/summary")
        {
            Content = JsonContent.Create(new GetOrderSummaryPagedRequest
            {
                Page = 1,
                PageSize = 50,
                SortColumn = "customerName",
                SortDirection = "asc"
            }),
        };

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<OrderSummaryPagedResponse>();
        var names = body!.Items.Select(r => r.CustomerName).ToList();
        names.Should().BeInAscendingOrder();
    }

    // ══════════════════════════════════════════════════════════════════════
    // Search: summary filters by search term
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetSummaryPaged_WithSearchTerm_ReturnsOnlyMatchingRows()
    {
        await SeedSummaryDataAsync(5);
        var client = CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders/summary")
        {
            Content = JsonContent.Create(new GetOrderSummaryPagedRequest
            {
                Page = 1,
                PageSize = 50,
                SortColumn = null,
                SortDirection = null,
                SearchTerm = "Seed Corp A"
            }),
        };

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<OrderSummaryPagedResponse>();
        body!.Items.Should().NotBeEmpty();
        body.Items.Should().AllSatisfy(r =>
            r.CustomerName.Should().Contain("Seed Corp A"));
    }

    // ══════════════════════════════════════════════════════════════════════
    // Export: CSV for all records (no search term)
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ExportSummaryCsv_WithNoFilter_ReturnsNonEmptyCsvWithCorrectHeaders()
    {
        await SeedSummaryDataAsync(3);
        var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/orders/summary/export",
            new ExportOrderSummaryCsvRequest());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");

        var csv = await response.Content.ReadAsStringAsync();
        var header = csv.Split('\n')[0];
        header.Should().Contain("OrderId");
        header.Should().Contain("CustomerName");
        header.Should().Contain("ProductName");
        header.Should().Contain("Quantity");
        header.Should().Contain("UnitPrice");
        header.Should().Contain("TotalAmount");
    }

    // ══════════════════════════════════════════════════════════════════════
    // Export: CSV filtered by search term contains only matching rows
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ExportSummaryCsv_WithSearchTerm_CsvContainsOnlyMatchingRows()
    {
        await SeedSummaryDataAsync(5);
        var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/orders/summary/export",
            new ExportOrderSummaryCsvRequest { SearchTerm = "Seed Corp B" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var csv = await response.Content.ReadAsStringAsync();
        using var reader  = new StringReader(csv);
        using var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture);
        var records = csvReader.GetRecords<dynamic>().ToList();

        records.Should().NotBeEmpty();
        foreach (IDictionary<string, object> row in records)
        {
            row["CustomerName"].ToString().Should().Contain("Seed Corp B");
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // Export: CSV row count matches summary TotalCount for same filter
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ExportSummaryCsv_RowCountMatchesSummaryTotalCount_ForSameFilter()
    {
        await SeedSummaryDataAsync(8);
        var client     = CreateClient();
        const string term = "Seed Corp C";

        // Query summary to get total count
        var summaryRequest = new HttpRequestMessage(HttpMethod.Post, "/api/orders/summary")
        {
            Content = JsonContent.Create(new GetOrderSummaryPagedRequest
            {
                Page = 1,
                PageSize = 1000,
                SortColumn = null,
                SortDirection = null,
                SearchTerm = term
            }),
        };

        var summaryResponse = await client.SendAsync(summaryRequest);
        var summary = await summaryResponse.Content.ReadFromJsonAsync<OrderSummaryPagedResponse>();
        var expectedCount = summary!.TotalCount;

        // Export with same filter
        var exportResponse = await client.PostAsJsonAsync(
            "/api/orders/summary/export",
            new ExportOrderSummaryCsvRequest { SearchTerm = term });

        var csv = await exportResponse.Content.ReadAsStringAsync();
        using var reader    = new StringReader(csv);
        using var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture);
        var csvRows = csvReader.GetRecords<dynamic>().ToList();

        csvRows.Should().HaveCount(expectedCount,
            because: "the CSV export must produce exactly the same number of rows as the paged query TotalCount");
    }
}
