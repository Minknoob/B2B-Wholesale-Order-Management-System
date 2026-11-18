IF DB_ID(N'B2BWholesale') IS NULL CREATE DATABASE B2BWholesale;
GO
USE B2BWholesale;
GO
IF OBJECT_ID(N'dbo.OrderDetail', N'U') IS NOT NULL DROP TABLE dbo.OrderDetail;
IF OBJECT_ID(N'dbo.[Order]', N'U') IS NOT NULL DROP TABLE dbo.[Order];
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID(N'dbo.Agent', N'U') IS NOT NULL DROP TABLE dbo.Agent;
IF OBJECT_ID(N'dbo.Item', N'U') IS NOT NULL DROP TABLE dbo.Item;
GO
CREATE TABLE dbo.Item (
    ItemID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Item PRIMARY KEY,
    ItemName NVARCHAR(120) NOT NULL,
    Size NVARCHAR(50) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    StockQuantity INT NOT NULL CONSTRAINT DF_Item_StockQuantity DEFAULT (0),
    CONSTRAINT CK_Item_Price CHECK (Price >= 0),
    CONSTRAINT CK_Item_StockQuantity CHECK (StockQuantity >= 0)
);
GO
CREATE TABLE dbo.Agent (
    AgentID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Agent PRIMARY KEY,
    AgentName NVARCHAR(120) NOT NULL,
    Address NVARCHAR(250) NOT NULL,
    Phone VARCHAR(20) NOT NULL
);
GO
CREATE TABLE dbo.[Order] (
    OrderID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Order PRIMARY KEY,
    OrderDate DATETIME2(0) NOT NULL CONSTRAINT DF_Order_OrderDate DEFAULT (SYSDATETIME()),
    AgentID INT NOT NULL,
    OrderStatus NVARCHAR(20) NOT NULL CONSTRAINT DF_Order_OrderStatus DEFAULT (N'Pending'),
    CONSTRAINT FK_Order_Agent FOREIGN KEY (AgentID) REFERENCES dbo.Agent(AgentID) ON DELETE NO ACTION ON UPDATE CASCADE,
    CONSTRAINT CK_Order_OrderStatus CHECK (OrderStatus IN (N'Pending',N'Confirmed',N'Shipped',N'Delivered',N'Cancelled'))
);
GO
CREATE TABLE dbo.OrderDetail (
    ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderDetail PRIMARY KEY,
    OrderID INT NOT NULL,
    ItemID INT NOT NULL,
    Quantity INT NOT NULL,
    UnitAmount DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_OrderDetail_Order FOREIGN KEY (OrderID) REFERENCES dbo.[Order](OrderID) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT FK_OrderDetail_Item FOREIGN KEY (ItemID) REFERENCES dbo.Item(ItemID) ON DELETE NO ACTION ON UPDATE CASCADE,
    CONSTRAINT CK_OrderDetail_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_OrderDetail_UnitAmount CHECK (UnitAmount >= 0)
);
GO
CREATE TABLE dbo.Users (
    UserID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    UserName NVARCHAR(50) NOT NULL,
    Email NVARCHAR(150) NOT NULL,
    PasswordHash NVARCHAR(300) NOT NULL,
    IsLocked BIT NOT NULL CONSTRAINT DF_Users_IsLocked DEFAULT (0),
    Role NVARCHAR(20) NOT NULL,
    AgentID INT NULL,
    FailedLoginCount INT NOT NULL CONSTRAINT DF_Users_FailedLoginCount DEFAULT (0),
    CONSTRAINT UQ_Users_UserName UNIQUE (UserName),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT FK_Users_Agent FOREIGN KEY (AgentID) REFERENCES dbo.Agent(AgentID) ON DELETE NO ACTION ON UPDATE CASCADE,
    CONSTRAINT CK_Users_Role CHECK (Role IN (N'Admin',N'Agent')),
    CONSTRAINT CK_Users_FailedLoginCount CHECK (FailedLoginCount >= 0),
    CONSTRAINT CK_Users_RoleAgent CHECK ((Role = N'Admin' AND AgentID IS NULL) OR (Role = N'Agent' AND AgentID IS NOT NULL))
);
GO
-- FK rationale: Order.AgentID preserves the agent reference for historical orders; OrderDetail.OrderID
-- cascades because details have no meaning without their parent order; OrderDetail.ItemID restricts
-- deleting products already used in orders; Users.AgentID keeps account-to-agent mapping valid.
