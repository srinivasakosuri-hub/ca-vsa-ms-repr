using FluentAssertions;
using FluentValidation.Results;
using TemplateMs.Application.Features.Order.Commands;
using TemplateMs.Application.Features.Order.Queries;
using Xunit;

namespace TemplateMs.Application.Tests;

/// <summary>Unit tests for FluentValidation validators in the Order feature.</summary>
public sealed class ValidationTests
{
    private readonly CreateOrderCommandValidator _createValidator = new();
    private readonly GetOrderQueryValidator _getValidator = new();
    private static readonly DateTime ValidOrderDate = new(2025, 5, 20, 0, 0, 0, DateTimeKind.Utc);

    // CreateOrderCommandValidator                     

    [Fact]
    public async Task CreateOrderCommandValidatorWithValidDataPassesValidation()
    {
        var command = new CreateOrderCommand("John Smith", ValidOrderDate);

        var result = await _createValidator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CreateOrderCommandValidatorWithEmptyCustomerNameFailsValidation()
    {
        var command = new CreateOrderCommand(string.Empty, ValidOrderDate);

        var result = await _createValidator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateOrderCommand.CustomerName));
    }

    [Fact]
    public async Task CreateOrderCommandValidatorWithWhitespaceCustomerNameFailsValidation()
    {
        var command = new CreateOrderCommand("   ", ValidOrderDate);

        var result = await _createValidator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task CreateOrderCommandValidatorWithDefaultOrderDateFailsValidation()
    {
        var command = new CreateOrderCommand("John Smith", default);

        var result = await _createValidator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateOrderCommand.OrderDate));
    }

    [Fact]
    public async Task CreateOrderCommandValidatorWithValidItemsAndAttributesPassesValidation()
    {
        var command = new CreateOrderCommand("John Smith", ValidOrderDate,
        [
            new CreateOrderItemData("Widget", 2, 9.99m,
            [
                new CreateOrderItemAttributeData("Color", "Red")
            ])
        ]);

        var result = await _createValidator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CreateOrderCommandValidatorWithEmptyProductNameFailsValidation()
    {
        var command = new CreateOrderCommand("John Smith", ValidOrderDate,
        [
            new CreateOrderItemData(string.Empty, 1, 5m)
        ]);

        var result = await _createValidator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("ProductName"));
    }

    [Fact]
    public async Task CreateOrderCommandValidatorWithZeroQuantityFailsValidation()
    {
        var command = new CreateOrderCommand("John Smith", ValidOrderDate,
        [
            new CreateOrderItemData("Widget", 0, 5m)
        ]);

        var result = await _createValidator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Quantity"));
    }

    [Fact]
    public async Task CreateOrderCommandValidatorWithNegativeUnitPriceFailsValidation()
    {
        var command = new CreateOrderCommand("John Smith", ValidOrderDate,
        [
            new CreateOrderItemData("Widget", 1, -1m)
        ]);

        var result = await _createValidator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("UnitPrice"));
    }

    [Fact]
    public async Task CreateOrderCommandValidatorWithEmptyAttributeNameFailsValidation()
    {
        var command = new CreateOrderCommand("John Smith", ValidOrderDate,
        [
            new CreateOrderItemData("Widget", 1, 5m,
            [
                new CreateOrderItemAttributeData(string.Empty, "Blue")
            ])
        ]);

        var result = await _createValidator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("AttributeName"));
    }

    [Fact]
    public async Task CreateOrderCommandValidatorWithEmptyAttributeValueFailsValidation()
    {
        var command = new CreateOrderCommand("John Smith", ValidOrderDate,
        [
            new CreateOrderItemData("Widget", 1, 5m,
            [
                new CreateOrderItemAttributeData("Color", string.Empty)
            ])
        ]);

        var result = await _createValidator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("AttributeValue"));
    }

    // GetOrderQueryValidator 

    [Fact]
    public async Task GetOrderQueryValidatorWithValidIdPassesValidation()
    {
        var query = new GetOrderQuery(1);

        var result = await _getValidator.ValidateAsync(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task GetOrderQueryValidatorWithEmptyIdFailsValidation()
    {
        var query = new GetOrderQuery(0);

        var result = await _getValidator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
    }

    // ValidationResultExtensions                     

    [Fact]
    public void ThrowIfInvalidWithInvalidResultThrowsValidationException()
    {
        var result = new ValidationResult(new[]
        {
            new ValidationFailure("Field", "Error message")
        });

        var act = () => result.ThrowIfInvalid();

        act.Should().Throw<Application.Exceptions.ValidationException>();
    }

    [Fact]
    public void ThrowIfInvalidWithValidResultDoesNotThrow()
    {
        var result = new ValidationResult();

        var act = () => result.ThrowIfInvalid();

        act.Should().NotThrow();
    }

    // ══════════════════════════════════════════════════════════════════════
    // GetOrdersPagedQueryValidator
    // ══════════════════════════════════════════════════════════════════════

    private readonly GetOrdersPagedQueryValidator _pagedValidator = new();

    [Fact]
    public async Task GetOrdersPagedQueryValidatorWithValidInputPassesValidation()
    {
        var query = new GetOrdersPagedQuery(1, 25, "orderDate", "desc", null);

        var result = await _pagedValidator.ValidateAsync(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task GetOrdersPagedQueryValidatorWithNullSortColumnAndDirectionPassesValidation()
    {
        var query = new GetOrdersPagedQuery(1, 10, null, null, null);

        var result = await _pagedValidator.ValidateAsync(query);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetOrdersPagedQueryValidatorWithInvalidPageFailsValidation(int page)
    {
        var query = new GetOrdersPagedQuery(page, 10, null, null, null);

        var result = await _pagedValidator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetOrdersPagedQuery.Page));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50)]
    public async Task GetOrdersPagedQueryValidatorWithInvalidPageSizeFailsValidation(int pageSize)
    {
        var query = new GetOrdersPagedQuery(1, pageSize, null, null, null);

        var result = await _pagedValidator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetOrdersPagedQuery.PageSize));
    }

    [Theory]
    [InlineData("orderId")]
    [InlineData("customerName")]
    [InlineData("orderDate")]
    [InlineData("productName")]
    [InlineData("quantity")]
    [InlineData("unitPrice")]
    [InlineData("totalAmount")]
    public async Task GetOrdersPagedQueryValidatorWithAllAllowedSortColumnsPassesValidation(string sortColumn)
    {
        var query = new GetOrdersPagedQuery(1, 10, sortColumn, "asc", null);

        var result = await _pagedValidator.ValidateAsync(query);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("OrderId")]       // wrong casing is still valid (case-insensitive whitelist)
    [InlineData("CUSTOMERNAME")]
    [InlineData("ORDERDATE")]
    public async Task GetOrdersPagedQueryValidatorSortColumnIsValidatedCaseInsensitively(string sortColumn)
    {
        var query = new GetOrdersPagedQuery(1, 10, sortColumn, "asc", null);

        var result = await _pagedValidator.ValidateAsync(query);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("invalidColumn")]
    [InlineData("createdAt")]
    [InlineData("id")]
    [InlineData("name")]
    public async Task GetOrdersPagedQueryValidatorWithUnknownSortColumnFailsValidation(string sortColumn)
    {
        var query = new GetOrdersPagedQuery(1, 10, sortColumn, "asc", null);

        var result = await _pagedValidator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetOrdersPagedQuery.SortColumn));
    }

    [Theory]
    [InlineData("asc")]
    [InlineData("desc")]
    [InlineData("ASC")]
    [InlineData("DESC")]
    public async Task GetOrdersPagedQueryValidatorWithValidSortDirectionPassesValidation(string sortDirection)
    {
        var query = new GetOrdersPagedQuery(1, 10, null, sortDirection, null);

        var result = await _pagedValidator.ValidateAsync(query);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("ascending")]
    [InlineData("descending")]
    [InlineData("1")]
    [InlineData("up")]
    public async Task GetOrdersPagedQueryValidatorWithInvalidSortDirectionFailsValidation(string sortDirection)
    {
        var query = new GetOrdersPagedQuery(1, 10, null, sortDirection, null);

        var result = await _pagedValidator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetOrdersPagedQuery.SortDirection));
    }

    [Fact]
    public async Task GetOrdersPagedQueryValidatorWithSearchTermAtMaxLengthPassesValidation()
    {
        var searchTerm = new string('a', GetOrdersPagedQueryValidator.MaxSearchTermLength);
        var query      = new GetOrdersPagedQuery(1, 10, null, null, searchTerm);

        var result = await _pagedValidator.ValidateAsync(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task GetOrdersPagedQueryValidatorWithSearchTermExceedingMaxLengthFailsValidation()
    {
        var searchTerm = new string('a', GetOrdersPagedQueryValidator.MaxSearchTermLength + 1);
        var query      = new GetOrdersPagedQuery(1, 10, null, null, searchTerm);

        var result = await _pagedValidator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetOrdersPagedQuery.SearchTerm));
    }

    [Fact]
    public async Task GetOrdersPagedQueryValidatorWithAllFieldsPopulatedAndValidPassesValidation()
    {
        var query = new GetOrdersPagedQuery(2, 50, "productName", "asc", "Widget");

        var result = await _pagedValidator.ValidateAsync(query);

        result.IsValid.Should().BeTrue();
    }
}

file static class ValidationResultExtensionsForTests
{
    public static void ThrowIfInvalid(this FluentValidation.Results.ValidationResult result)
    {
        if (!result.IsValid)
            throw new TemplateMs.Application.Exceptions.ValidationException(result.Errors);
    }
}
