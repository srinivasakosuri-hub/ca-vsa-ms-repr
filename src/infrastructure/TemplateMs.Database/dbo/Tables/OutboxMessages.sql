CREATE TABLE [dbo].[OutboxMessages] (
    [Id]          UNIQUEIDENTIFIER NOT NULL,
    [MessageType] NVARCHAR (500)   NOT NULL,
    [Payload]     NVARCHAR (MAX)   NOT NULL,
    [CreatedAt]   DATETIME2 (7)    NOT NULL,
    [ProcessedAt] DATETIME2 (7)    NULL,
    [Error]       NVARCHAR (MAX)   NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC)
);

