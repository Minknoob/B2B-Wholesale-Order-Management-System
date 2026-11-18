USE B2BWholesale;
GO
-- Expected: this INSERT fails because OrderID 999999 does not exist.
BEGIN TRY
    INSERT dbo.OrderDetail(OrderID,ItemID,Quantity,UnitAmount) VALUES (999999,1,1,1000);
    PRINT 'ERROR: FK test unexpectedly passed.';
END TRY
BEGIN CATCH
    PRINT 'PASS: invalid OrderID was rejected by the foreign key.';
    PRINT ERROR_MESSAGE();
END CATCH;
GO
-- Expected: negative stock and zero quantity are rejected.
BEGIN TRY
    UPDATE dbo.Item SET StockQuantity=-1 WHERE ItemID=1;
    PRINT 'ERROR: negative stock unexpectedly passed.';
END TRY
BEGIN CATCH
    PRINT 'PASS: negative stock was rejected.';
END CATCH;
GO
BEGIN TRY
    INSERT dbo.OrderDetail(OrderID,ItemID,Quantity,UnitAmount) VALUES (1,1,0,1000);
    PRINT 'ERROR: zero quantity unexpectedly passed.';
END TRY
BEGIN CATCH
    PRINT 'PASS: zero quantity was rejected.';
END CATCH;
GO
-- Expected: deleting an Agent that already has orders is blocked (ON DELETE NO ACTION).
BEGIN TRY
    DELETE FROM dbo.Agent WHERE AgentID = 1;
    PRINT 'ERROR: deleting an agent with orders unexpectedly passed.';
END TRY
BEGIN CATCH
    PRINT 'PASS: deleting an agent that has orders was blocked.';
END CATCH;
GO
-- Expected: deleting an Item already used in an order is blocked.
BEGIN TRY
    DELETE FROM dbo.Item WHERE ItemID = 1;
    PRINT 'ERROR: deleting a used item unexpectedly passed.';
END TRY
BEGIN CATCH
    PRINT 'PASS: deleting an item used in an order was blocked.';
END CATCH;
GO
-- Expected: duplicate UserName / Email are rejected (UNIQUE).
BEGIN TRY
    INSERT dbo.Users(UserName,Email,PasswordHash,Role) VALUES (N'admin',N'dup@b2b.local',N'x',N'Admin');
    PRINT 'ERROR: duplicate UserName unexpectedly passed.';
END TRY
BEGIN CATCH
    PRINT 'PASS: duplicate UserName was rejected.';
END CATCH;
GO
BEGIN TRY
    INSERT dbo.Users(UserName,Email,PasswordHash,Role) VALUES (N'newadmin',N'admin@b2b.local',N'x',N'Admin');
    PRINT 'ERROR: duplicate Email unexpectedly passed.';
END TRY
BEGIN CATCH
    PRINT 'PASS: duplicate Email was rejected.';
END CATCH;
GO
-- Expected: deleting an Order also deletes its OrderDetail rows (CASCADE). Rolled back, so seed data is untouched.
BEGIN TRAN;
    DECLARE @before INT = (SELECT COUNT(*) FROM dbo.OrderDetail WHERE OrderID = 1);
    DELETE FROM dbo.[Order] WHERE OrderID = 1;
    DECLARE @after INT = (SELECT COUNT(*) FROM dbo.OrderDetail WHERE OrderID = 1);
    IF @before > 0 AND @after = 0 PRINT 'PASS: deleting an order cascaded to its OrderDetail rows.';
    ELSE PRINT 'ERROR: cascade delete did not work.';
ROLLBACK;
GO
