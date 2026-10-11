CREATE TABLE [dbo].[OrderItemAttributes] (
    [Id] INT              IDENTITY (1, 1) NOT NULL,
    [OrderItemId]          INT              NOT NULL,
    [AttributeName]        VARCHAR (100)    NOT NULL,
    [AttributeValue]       VARCHAR (255)    NOT NULL,
    [IsActive]             BIT              NOT NULL,
    [CreatedBy]            UNIQUEIDENTIFIER NOT NULL,
    [CreatedAt]            DATETIME         NOT NULL,
    [UpdatedBy]            UNIQUEIDENTIFIER NOT NULL,
    [UpdatedAt]            DATETIME         NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK__OrderItem__Order__3E52440B] FOREIGN KEY ([OrderItemId]) REFERENCES [dbo].[OrderItems] ([Id])
);




GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Date last updated', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'OrderItemAttributes', @level2type = N'COLUMN', @level2name = N'UpdatedAt';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'User who last updated record', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'OrderItemAttributes', @level2type = N'COLUMN', @level2name = N'UpdatedBy';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Date created', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'OrderItemAttributes', @level2type = N'COLUMN', @level2name = N'CreatedAt';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'User who created record', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'OrderItemAttributes', @level2type = N'COLUMN', @level2name = N'CreatedBy';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Active/inactive indicator', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'OrderItemAttributes', @level2type = N'COLUMN', @level2name = N'IsActive';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Value of the attribute (e.g., Blue)', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'OrderItemAttributes', @level2type = N'COLUMN', @level2name = N'AttributeValue';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Name of the attribute (e.g., Color)', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'OrderItemAttributes', @level2type = N'COLUMN', @level2name = N'AttributeName';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Reference to OrderItems table', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'OrderItemAttributes', @level2type = N'COLUMN', @level2name = 'OrderItemId';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Primary key of OrderItemAttributes', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'OrderItemAttributes', @level2type = N'COLUMN', @level2name = 'Id';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Child table storing attribute details for each order item', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'OrderItemAttributes';

