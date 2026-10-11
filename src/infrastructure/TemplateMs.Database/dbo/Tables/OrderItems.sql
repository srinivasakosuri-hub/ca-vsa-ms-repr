CREATE TABLE [dbo].[OrderItems] (
    [Id] INT              IDENTITY (1, 1) NOT NULL,
    [OrderId]     INT              NOT NULL,
    [ProductName] VARCHAR (100)    NOT NULL,
    [Quantity]    INT              NOT NULL,
    [UnitPrice]   DECIMAL (18, 2)  NOT NULL,
    [TotalAmount] AS               ([Quantity]*[UnitPrice]),
    [IsActive]    BIT              NOT NULL,
    [CreatedBy]   UNIQUEIDENTIFIER NOT NULL,
    [CreatedAt]   DATETIME         NOT NULL,
    [UpdatedBy]   UNIQUEIDENTIFIER NOT NULL,
    [UpdatedAt]   DATETIME         NOT NULL,
    CONSTRAINT [PK__OrderIte__57ED06A11AAE5684] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [CK__OrderItem__Quant__3B75D760] CHECK ([Quantity]>(0)),
    CONSTRAINT [FK__OrderItem__Order__3A81B327] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders] ([Id])
);




GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Date last updated', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'OrderItems', @level2type = N'COLUMN', @level2name = N'UpdatedAt';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Last updated by user', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'OrderItems', @level2type = N'COLUMN', @level2name = N'UpdatedBy';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Date created', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'OrderItems', @level2type = N'COLUMN', @level2name = N'CreatedAt';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Record created by user', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'OrderItems', @level2type = N'COLUMN', @level2name = N'CreatedBy';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Indicates if the record is active', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'OrderItems', @level2type = N'COLUMN', @level2name = N'IsActive';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Automatically calculated total amount', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'OrderItems', @level2type = N'COLUMN', @level2name = N'TotalAmount';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Price per unit of this item', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'OrderItems', @level2type = N'COLUMN', @level2name = N'UnitPrice';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Number of units ordered', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'OrderItems', @level2type = N'COLUMN', @level2name = N'Quantity';


GO



GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Foreign key to Orders table', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'OrderItems', @level2type = N'COLUMN', @level2name = 'OrderId';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Primary key for OrderItems', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'OrderItems', @level2type = N'COLUMN', @level2name = 'Id';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Table storing individual items belonging to an order', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'OrderItems';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Product identifier', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'OrderItems', @level2type = N'COLUMN', @level2name = N'ProductName';

