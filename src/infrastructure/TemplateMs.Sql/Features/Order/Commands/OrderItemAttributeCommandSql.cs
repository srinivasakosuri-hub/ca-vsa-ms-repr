namespace TemplateMs.Sql.Features.Order.Commands;

/// <summary>Contains all SQL command statements for the OrderItemAttributes table.</summary>
public static class OrderItemAttributeCommandSql
{
    /// <summary>Inserts a new attribute row and returns the generated Id surrogate key.</summary>
    public const string Insert = """
        INSERT INTO OrderItemAttributes (OrderItemId, AttributeName, AttributeValue, IsActive, CreatedBy, CreatedAt, UpdatedBy, UpdatedAt)
        OUTPUT INSERTED.Id
        VALUES (@OrderItemId, @AttributeName, @AttributeValue, @IsActive, @CreatedBy, @CreatedAt, @UpdatedBy, @UpdatedAt);
        """;

    /// <summary>Soft-deletes an attribute by setting IsActive=0 and stamping UpdatedAt.</summary>
    public const string Deactivate = """
        UPDATE OrderItemAttributes
        SET    IsActive  = 0,
               UpdatedAt = @UpdatedAt
        WHERE  Id        = @Id;
        """;
}
