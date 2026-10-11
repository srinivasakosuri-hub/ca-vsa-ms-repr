namespace TemplateMs.Sql.Infrastructure.Outbox;

/// <summary>SQL statements for the custom outbox table used to stage integration events before AMQ publishing.</summary>
public static class OutboxSql
{
    /// <summary>Inserts a new outbox record within the command transaction.</summary>
    public const string Insert = """
        INSERT INTO OutboxMessages (Id, MessageType, Payload, CreatedAt, ProcessedAt, Error)
        VALUES (@Id, @MessageType, @Payload, @CreatedAt, NULL, NULL);
        """;

    /// <summary>Selects unprocessed outbox records for the background processor.</summary>
    public const string SelectUnprocessed = """
        SELECT TOP (@BatchSize) Id, MessageType, Payload, CreatedAt
        FROM   OutboxMessages
        WHERE  ProcessedAt IS NULL
        ORDER BY CreatedAt ASC;
        """;

    /// <summary>Marks an outbox record as successfully processed.</summary>
    public const string MarkProcessed = """
        UPDATE OutboxMessages
        SET    ProcessedAt = @ProcessedAt
        WHERE  Id = @Id;
        """;

    /// <summary>Marks an outbox record as failed with an error message.</summary>
    public const string MarkFailed = """
        UPDATE OutboxMessages
        SET    Error = @Error
        WHERE  Id = @Id;
        """;
}
