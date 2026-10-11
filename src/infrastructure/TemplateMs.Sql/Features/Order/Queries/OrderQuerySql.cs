namespace TemplateMs.Sql.Features.Order.Queries;

/// <summary>Contains all SQL query statements for the Orders table. No inline SQL should exist outside this class.</summary>
public static class OrderQuerySql
{
    /// <summary>
    /// Returns the order row plus all child item and attribute rows in a single multi-result-set call.
    /// Grid 1: Order columns. Grid 2: OrderItems columns. Grid 3: OrderItemAttributes columns.
    /// </summary>
    public const string GetById = @"
        SELECT o.Id, o.CustomerName, o.OrderDate, o.IsActive, o.CreatedBy, o.CreatedAt, o.UpdatedBy, o.UpdatedAt
        FROM   Orders o
        WHERE  o.Id = @Id;

        SELECT i.Id, i.OrderId, i.ProductName, i.Quantity, i.UnitPrice, i.IsActive,
               i.CreatedBy, i.CreatedAt, i.UpdatedBy, i.UpdatedAt
        FROM   OrderItems i
        WHERE  i.OrderId = @Id;

        SELECT a.Id, a.OrderItemId, a.AttributeName, a.AttributeValue, a.IsActive,
               a.CreatedBy, a.CreatedAt, a.UpdatedBy, a.UpdatedAt
        FROM   OrderItemAttributes a
        INNER JOIN OrderItems i ON i.Id = a.OrderItemId
        WHERE  i.OrderId = @Id;
        ";

    /// <summary>Selects all orders ordered by creation date descending (header row only, no items).</summary>
    public const string GetAll = @"
        SELECT o.Id, o.CustomerName, o.OrderDate, o.IsActive, o.CreatedBy, o.CreatedAt, o.UpdatedBy, o.UpdatedAt
        FROM   Orders o
        ORDER BY o.CreatedAt DESC;
        ";

    /// <summary>
    /// Paged order-summary query with optional full-text search. Returns two result sets in a single round-trip:
    /// <list type="number">
    ///   <item>Grid 1 — total record count matching the current filter (scalar).</item>
    ///   <item>Grid 2 — one denormalised row per order line item for the requested page.</item>
    /// </list>
    /// <para>
    /// Format placeholders (resolved by <c>OrderReadRepository</c> before execution — never raw user text):
    /// <list type="bullet">
    ///   <item><c>{0}</c> — whitelisted ORDER BY clause (e.g. <c>o.CustomerName ASC</c>).</item>
    ///   <item><c>{1}</c> — search predicate: <c>1=1</c> when no term, or a parameterised LIKE block.</item>
    /// </list>
    /// <c>@SearchTerm</c> is a Dapper parameter bound to <c>%term%</c> — never concatenated into SQL.
    /// </para>
    /// </summary>
    public const string GetPagedSummaryTemplate = @"
        SELECT COUNT_BIG(*) AS TotalCount
        FROM   Orders       o
        INNER JOIN OrderItems i ON i.OrderId = o.Id
        WHERE  o.IsActive = 1
          AND  i.IsActive = 1
          AND  {1};

        SELECT o.Id                              AS OrderId,
               o.CustomerName,
               o.OrderDate,
               i.Id                              AS OrderItemId,
               i.ProductName,
               i.Quantity,
               i.UnitPrice,
               CAST(i.Quantity AS DECIMAL(18,4)) * i.UnitPrice AS TotalAmount
        FROM   Orders       o
        INNER JOIN OrderItems i ON i.OrderId = o.Id
        WHERE  o.IsActive = 1
          AND  i.IsActive = 1
          AND  {1}
        ORDER BY {0}
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
        ";

    /// <summary>
    /// Search predicate used when a <c>SearchTerm</c> is supplied.
    /// Applied as a case-insensitive LIKE across CustomerName, ProductName and OrderDate.
    /// The value is bound via a Dapper parameter (<c>@SearchTerm</c> = <c>%term%</c>).
    /// </summary>
    public const string SearchPredicate =
        "(o.CustomerName LIKE @SearchTerm OR i.ProductName LIKE @SearchTerm OR CONVERT(VARCHAR(10), o.OrderDate, 120) LIKE @SearchTerm)";

    /// <summary>Predicate used when no search term is supplied — matches all rows.</summary>
    public const string NoSearchPredicate = "1=1";

    /// <summary>
    /// Export query: returns all order-summary rows matching the optional search filter,
    /// sorted by the whitelisted ORDER BY clause. No paging — every matching row is returned.
    /// <para>
    /// Format placeholders (resolved by <c>OrderReadRepository</c> before execution — never raw user text):
    /// <list type="bullet">
    ///   <item><c>{0}</c> — whitelisted ORDER BY clause (e.g. <c>o.CustomerName ASC</c>).</item>
    ///   <item><c>{1}</c> — search predicate: <c>1=1</c> when no term, or a parameterised LIKE block.</item>
    /// </list>
    /// <c>@SearchTerm</c> is a Dapper parameter bound to <c>%term%</c> — never concatenated into SQL.
    /// </para>
    /// </summary>
    public const string GetAllSummaryTemplate = @"
        SELECT o.Id                              AS OrderId,
               o.CustomerName,
               o.OrderDate,
               i.Id                              AS OrderItemId,
               i.ProductName,
               i.Quantity,
               i.UnitPrice,
               CAST(i.Quantity AS DECIMAL(18,4)) * i.UnitPrice AS TotalAmount
        FROM   Orders       o
        INNER JOIN OrderItems i ON i.OrderId = o.Id
        WHERE  o.IsActive = 1
          AND  i.IsActive = 1
          AND  {1}
        ORDER BY {0};
        ";
}

