USE B2BWholesale;
GO
SET NOCOUNT ON;

PRINT '=== B2B WHOLESALE - M3 TRANSACTION & STOCK ATOMICITY VERIFICATION ===';
PRINT 'Execution Timestamp: ' + CONVERT(VARCHAR(33), SYSDATETIMEOFFSET(), 126);
PRINT '';

-- 1. Verify conditional atomic stock update succeeds when StockQuantity >= @qty (rolled back to preserve seed data)
BEGIN TRAN;
    DECLARE @StockBefore1 INT = (SELECT StockQuantity FROM dbo.Item WHERE ItemID = 1);
    UPDATE dbo.Item
    SET StockQuantity = StockQuantity - 5
    WHERE ItemID = 1 AND StockQuantity >= 5;

    DECLARE @Affected1 INT = @@ROWCOUNT;
    DECLARE @StockAfter1 INT = (SELECT StockQuantity FROM dbo.Item WHERE ItemID = 1);

    IF @Affected1 = 1 AND @StockAfter1 = @StockBefore1 - 5
        PRINT CONCAT('PASS [SQL-01]: Sufficient stock atomic deduction succeeded (Stock: ', @StockBefore1, ' -> ', @StockAfter1, ').');
    ELSE
        PRINT 'ERROR [SQL-01]: Atomic deduction failed unexpectedly.';
ROLLBACK;
GO

-- 2. Verify conditional atomic stock update affects 0 rows when requested Quantity > StockQuantity
BEGIN TRAN;
    DECLARE @StockBefore2 INT = (SELECT StockQuantity FROM dbo.Item WHERE ItemID = 15);
    UPDATE dbo.Item
    SET StockQuantity = StockQuantity - 999
    WHERE ItemID = 15 AND StockQuantity >= 999;

    DECLARE @Affected2 INT = @@ROWCOUNT;
    DECLARE @StockAfter2 INT = (SELECT StockQuantity FROM dbo.Item WHERE ItemID = 15);

    IF @Affected2 = 0 AND @StockAfter2 = @StockBefore2
        PRINT CONCAT('PASS [SQL-02]: Insufficient stock blocked atomically (AffectedRows = 0, Stock unchanged at ', @StockAfter2, ').');
    ELSE
        PRINT 'ERROR [SQL-02]: Insufficient stock was not blocked.';
ROLLBACK;
GO

-- 3. Verify multi-line Master-Detail Order rollback when Line 1 succeeds and Line 2 lacks stock
DECLARE @OrdersBefore INT = (SELECT COUNT(*) FROM dbo.[Order]);
DECLARE @DetailsBefore INT = (SELECT COUNT(*) FROM dbo.OrderDetail);
DECLARE @Item4Before INT = (SELECT StockQuantity FROM dbo.Item WHERE ItemID = 4);
DECLARE @Item15Before INT = (SELECT StockQuantity FROM dbo.Item WHERE ItemID = 15);

BEGIN TRAN;
    INSERT INTO dbo.[Order] (AgentID, OrderStatus) VALUES (1, N'Pending');
    DECLARE @NewOrderId INT = SCOPE_IDENTITY();

    INSERT INTO dbo.OrderDetail (OrderID, ItemID, Quantity, UnitAmount)
    VALUES (@NewOrderId, 4, 5, 32000.00),
           (@NewOrderId, 15, 500, 68000.00);

    -- Deduct Line 1 (succeeds)
    UPDATE dbo.Item SET StockQuantity = StockQuantity - 5 WHERE ItemID = 4 AND StockQuantity >= 5;
    DECLARE @Row1 INT = @@ROWCOUNT;

    -- Deduct Line 2 (insufficient stock -> affects 0 rows -> triggers full rollback)
    UPDATE dbo.Item SET StockQuantity = StockQuantity - 500 WHERE ItemID = 15 AND StockQuantity >= 500;
    DECLARE @Row2 INT = @@ROWCOUNT;

    IF @Row1 <> 1 OR @Row2 <> 1
        ROLLBACK TRAN;
    ELSE
        COMMIT TRAN;

DECLARE @OrdersAfter INT = (SELECT COUNT(*) FROM dbo.[Order]);
DECLARE @DetailsAfter INT = (SELECT COUNT(*) FROM dbo.OrderDetail);
DECLARE @Item4After INT = (SELECT StockQuantity FROM dbo.Item WHERE ItemID = 4);
DECLARE @Item15After INT = (SELECT StockQuantity FROM dbo.Item WHERE ItemID = 15);

IF @OrdersBefore = @OrdersAfter
   AND @DetailsBefore = @DetailsAfter
   AND @Item4Before = @Item4After
   AND @Item15Before = @Item15After
    PRINT CONCAT('PASS [SQL-03]: Multi-line partial failure rolled back 100% (Orders=', @OrdersAfter, ', Details=', @DetailsAfter, ', Item4=', @Item4After, ', Item15=', @Item15After, ').');
ELSE
    PRINT 'ERROR [SQL-03]: Rollback left partial state behind.';
GO
