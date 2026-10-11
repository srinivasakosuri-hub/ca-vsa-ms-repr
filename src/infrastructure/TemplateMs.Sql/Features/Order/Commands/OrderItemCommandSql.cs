namespace TemplateMs.Sql.Features.Order.Commands;

/// <summary>Contains all SQL command statements for the OrderItems table.</summary>
public static class OrderItemCommandSql
{
    /// <summary>Inserts a new order item row and returns the generated Id surrogate key.</summary>
    public const string Insert = """
        INSERT INTO OrderItems (OrderId, ProductName, Quantity, UnitPrice, IsActive, CreatedBy, CreatedAt, UpdatedBy, UpdatedAt)
        OUTPUT INSERTED.Id
        VALUES (@OrderId, @ProductName, @Quantity, @UnitPrice, @IsActive, @CreatedBy, @CreatedAt, @UpdatedBy, @UpdatedAt);
        """;

    /// <summary>Soft-deletes an order item by setting IsActive=0 and stamping UpdatedAt.</summary>
    public const string Deactivate = """
        UPDATE OrderItems
        SET    IsActive  = 0,
               UpdatedAt = @UpdatedAt
        WHERE  Id        = @Id;
        """;
}
