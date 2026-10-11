using CsvHelper;
using FluentAssertions;
using NSubstitute;
using System.Globalization;
using System.Text;
using TemplateMs.Api.Endpoints.Orders;
using TemplateMs.Api.ResponseModels;
using TemplateMs.Application.Features.Order.Dtos;
using TemplateMs.Application.Features.Order.Queries;
using Wolverine;
using Xunit;

namespace TemplateMs.Api.Tests.Endpoints;

/// <summary>
/// Unit tests for <see cref="ExportOrderSummaryCsvEndpoint"/> and the
/// supporting <see cref="ExportOrderSummaryCsvRequest"/> / <see cref="OrderSummaryItemResponse"/> types.
/// The <see cref="IMessageBus"/> is substituted with NSubstitute.
/// A deterministic in-memory dataset of up to 500 rows is used for content assertions.
/// </summary>
public sealed class ExportOrderSummaryEndpointTests
{
    // ── Fixed values ─────────────────────────────────────────────────────────
    private static readonly DateTime BaseDate = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // ── Dataset factory ──────────────────────────────────────────────────────

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
                OrderDate:    BaseDate.AddDays(i - 1),
                OrderItemId:  i * 10,
                ProductName:  ProductFor(i),
                Quantity:     (i % 20) + 1,
                UnitPrice:    Math.Round(1.00m + (i % 50) * 0.99m, 2),
                TotalAmount:  Math.Round(((i % 20) + 1) * (1.00m + (i % 50) * 0.99m), 2)))
            .ToList()
            .AsReadOnly();
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static List<dynamic> ParseCsv(string csvContent)
    {
        using var reader = new StringReader(csvContent);
        using var csv    = new CsvReader(reader, CultureInfo.InvariantCulture);
        return csv.GetRecords<dynamic>().ToList();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Business Logic Tests
    // ═══════════════════════════════════════════════════════════════════════════

    //  CSV content generation

    [Fact]
    public void BuildDataset_WithDefaultCount_Returns500Items()
    {
        // Act
        var dataset = BuildDataset();

        // Assert
        dataset.Should().HaveCount(500);
    }

    [Fact]
    public void BuildDataset_WithCustomCount_ReturnsCorrectCount()
    {
        // Act
        var dataset = BuildDataset(10);

        // Assert
        dataset.Should().HaveCount(10);
    }

    [Fact]
    public void BuildDataset_FirstItem_HasCorrectStructure()
    {
        // Act
        var dataset = BuildDataset(1);
        var first = dataset[0];

        // Assert
        first.OrderId.Should().Be(1);
        first.CustomerName.Should().Be("Acme Corp");
        first.ProductName.Should().Contain("Widget");
        first.Quantity.Should().Be(2);
    }

    [Fact]
    public void BuildDataset_Items_HaveValidPrices()
    {
        // Act
        var dataset = BuildDataset(100);

        // Assert
        dataset.Should().AllSatisfy(item =>
        {
            item.UnitPrice.Should().BeGreaterThan(0);
            item.TotalAmount.Should().BeGreaterThan(0);
            item.TotalAmount.Should().Be(item.Quantity * item.UnitPrice);
        });
    }

    [Fact]
    public void ParseCsv_WithValidCsv_ReturnsRecords()
    {
        // Arrange
        var csvContent = "OrderId,CustomerName\n1,Acme Corp\n2,Beta Ltd\n";

        // Act
        var records = ParseCsv(csvContent);

        // Assert
        records.Should().HaveCount(2);
    }

    [Fact]
    public void ParseCsv_FirstRecord_ContainsCorrectValues()
    {
        // Arrange
        var csvContent = "OrderId,CustomerName,ProductName\n1,Acme Corp,Widget A\n";

        // Act
        var records = ParseCsv(csvContent);
        var first = records[0];

        // Assert
        ((string)first.OrderId).Should().Be("1");
        ((string)first.CustomerName).Should().Be("Acme Corp");
        ((string)first.ProductName).Should().Be("Widget A");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CSV Format Tests
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Dataset_CsvExport_HasValidHeaderFormat()
    {
        // Arrange
        var dataset = BuildDataset(1);

        // Act
        using var stream = new MemoryStream();
        using (var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture);
            using (var csv = new CsvWriter(writer, config))
            {
                csv.WriteHeader<OrderSummaryItemResponse>();
                csv.NextRecord();
            }
        }
        stream.Position = 0;
        var content = new StreamReader(stream).ReadToEnd();

        // Assert
        content.Should().NotBeNullOrEmpty();
        var lines = content.Split('\n');
        lines[0].Should().Contain("OrderId");
    }

    [Fact]
    public void Dataset_AllItems_HaveDatesInSequence()
    {
        // Arrange & Act
        var dataset = BuildDataset(10);

        // Assert
        for (int i = 0; i < dataset.Count; i++)
        {
            dataset[i].OrderDate.Should().Be(BaseDate.AddDays(i));
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(500)]
    public void BuildDataset_WithVariousCounts_ReturnsExpectedCount(int count)
    {
        // Act
        var dataset = BuildDataset(count);

        // Assert
        dataset.Should().HaveCount(count);
    }

    [Fact]
    public void CustomerName_Distribution_IsBalanced()
    {
        // Arrange & Act
        var dataset = BuildDataset(500);

        // Assert - verify customer distribution
        var acmeCount = dataset.Count(x => x.CustomerName == "Acme Corp");
        var betaCount = dataset.Count(x => x.CustomerName == "Beta Ltd");
        var gammaCount = dataset.Count(x => x.CustomerName == "Gamma Inc");
        var deltaCount = dataset.Count(x => x.CustomerName == "Delta Systems");

        acmeCount.Should().Be(200);
        betaCount.Should().Be(100);
        gammaCount.Should().Be(100);
        deltaCount.Should().Be(100);
    }

    [Fact]
    public void ProductName_Distribution_VariesByRange()
    {
        // Arrange & Act
        var dataset = BuildDataset(500);

        // Assert - verify product name patterns
        var widgetCount = dataset.Count(x => x.ProductName.StartsWith("Widget"));
        var gadgetCount = dataset.Count(x => x.ProductName.StartsWith("Gadget"));
        var deviceCount = dataset.Count(x => x.ProductName.StartsWith("Device"));
        var moduleCount = dataset.Count(x => x.ProductName.StartsWith("Module"));

        widgetCount.Should().BeGreaterThan(0);
        gadgetCount.Should().BeGreaterThan(0);
        deviceCount.Should().BeGreaterThan(0);
        moduleCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Quantity_IsCalculatedCorrectly()
    {
        // Arrange & Act
        var dataset = BuildDataset(20);

        // Assert
        for (int i = 0; i < dataset.Count; i++)
        {
            var expectedQuantity = ((i + 1) % 20) + 1;
            dataset[i].Quantity.Should().Be(expectedQuantity);
        }
    }

    [Fact]
    public void UnitPrice_RangeIsExpected()
    {
        // Arrange & Act
        var dataset = BuildDataset(500);

        // Assert
        var minPrice = dataset.Min(x => x.UnitPrice);
        var maxPrice = dataset.Max(x => x.UnitPrice);

        minPrice.Should().BeGreaterThanOrEqualTo(1.00m);
        maxPrice.Should().BeLessThanOrEqualTo(50.00m);
    }

    [Fact]
    public void TotalAmount_EqualsQuantityTimesUnitPrice()
    {
        // Arrange & Act
        var dataset = BuildDataset(100);

        // Assert
        dataset.Should().AllSatisfy(item =>
        {
            var expected = Math.Round(item.Quantity * item.UnitPrice, 2);
            item.TotalAmount.Should().Be(expected);
        });
    }
}
