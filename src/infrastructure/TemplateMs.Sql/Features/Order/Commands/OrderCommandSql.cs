namespace TemplateMs.Sql.Features.Order.Commands;

/// <summary>Contains all SQL command statements for the Orders table. No inline SQL should exist outside this class.</summary>
public static class OrderCommandSql
{
    /// <summary>Inserts a new order row and returns the generated Id surrogate key.</summary>
    public const string Insert = @"
        INSERT INTO Orders (CustomerName, OrderDate, IsActive, CreatedBy, CreatedAt, UpdatedBy, UpdatedAt)
        OUTPUT INSERTED.Id
        VALUES (@CustomerName, @OrderDate, @IsActive, @CreatedBy, @CreatedAt, @UpdatedBy, @UpdatedAt);
        ";

    /// <summary>Updates the IsActive and UpdatedAt fields of an existing order.</summary>
    public const string UpdateStatus = @"
        UPDATE Orders
        SET    IsActive  = @IsActive,
               UpdatedBy = @UpdatedBy,
               UpdatedAt = @UpdatedAt
        WHERE  Id        = @Id;
        ";

    /// <summary>Selects the Id surrogate key for a given Id.</summary>
    public const string GetOrderIdByDomainId = @"
        SELECT Id FROM Orders WHERE Id = @Id;
        ";
}
