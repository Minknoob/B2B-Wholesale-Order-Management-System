USE B2BWholesale;
GO
SET IDENTITY_INSERT dbo.Item ON;
INSERT dbo.Item(ItemID,ItemName,Size,Price,StockQuantity) VALUES
(1,N'Rice 5kg',N'5 kg',95000,120),(2,N'Cooking Oil',N'1 L',42000,85),(3,N'Sugar',N'1 kg',26000,70),
(4,N'Fish Sauce',N'500 ml',32000,95),(5,N'Instant Noodles',N'Pack 5',18000,200),(6,N'Coffee',N'500 g',115000,45),
(7,N'Milk',N'1 L',34000,110),(8,N'Tea',N'Hộp 200 g',56000,60),(9,N'Salt',N'1 kg',12000,150),
(10,N'Soy Sauce',N'500 ml',25000,90),(11,N'Detergent',N'2 kg',78000,35),(12,N'Dishwashing Liquid',N'750 ml',39000,55),
(13,N'Tissue',N'10 rolls',52000,80),(14,N'Canned Tuna',N'185 g',29000,100),(15,N'Oatmeal',N'1 kg',68000,40);
SET IDENTITY_INSERT dbo.Item OFF;
GO
SET IDENTITY_INSERT dbo.Agent ON;
INSERT dbo.Agent(AgentID,AgentName,Address,Phone) VALUES
(1,N'An Phat Trading',N'12 Nguyen Trai, District 1, HCMC','0901000001'),
(2,N'Minh Long Mart',N'25 Le Loi, Thu Duc, HCMC','0901000002'),
(3,N'Gia Bao Distribution',N'88 Cach Mang Thang 8, HCMC','0901000003'),
(4,N'Thanh Cong Store',N'10 Tran Hung Dao, Da Nang','0901000004'),
(5,N'Phuong Nam Wholesale',N'17 Hai Ba Trung, Can Tho','0901000005'),
(6,N'Hoang Gia Market',N'9 Hung Vuong, Hue','0901000006'),
(7,N'Tan Phu Supplies',N'45 Au Co, HCMC','0901000007'),
(8,N'Dai Phat Wholesale',N'21 Phan Xich Long, HCMC','0901000008'),
(9,N'Nam Viet Trading',N'33 Nguyen Van Linh, Da Nang','0901000009'),
(10,N'Binh Minh Supplies',N'18 Vo Van Tan, HCMC','0901000010'),
(11,N'Thuan Phat Mart',N'6 Le Duan, Can Tho','0901000011'),
(12,N'An Khang Distribution',N'72 Tran Phu, Nha Trang','0901000012'),
(13,N'Phuc Loc Store',N'15 Hung Vuong, Bien Hoa','0901000013'),
(14,N'Thanh Dat Wholesale',N'90 Quang Trung, HCMC','0901000014'),
(15,N'Viet Hung Trading',N'8 Nguyen Hue, Vung Tau','0901000015');
SET IDENTITY_INSERT dbo.Agent OFF;
GO
-- Seed password for all sample users (1 Admin + 14 Agents = 15 rows): Demo@123. Hash format is PBKDF2-SHA256, 120000 iterations.
SET IDENTITY_INSERT dbo.Users ON;
INSERT dbo.Users(UserID,UserName,Email,PasswordHash,IsLocked,Role,AgentID,FailedLoginCount) VALUES
(1,N'admin',N'admin@b2b.local',N'pbkdf2$120000$QjJCRGVtb1NhbHQxMjM0NTY3OA==$moKf/amkFlpGZrA9a//CrTtILZrEVsRFu2FIZfiLrrY=',0,N'Admin',NULL,0),
(2,N'agent01',N'agent01@b2b.local',N'pbkdf2$120000$QjJCRGVtb1NhbHQxMjM0NTY3OA==$moKf/amkFlpGZrA9a//CrTtILZrEVsRFu2FIZfiLrrY=',0,N'Agent',1,0),
(3,N'agent02',N'agent02@b2b.local',N'pbkdf2$120000$QjJCRGVtb1NhbHQxMjM0NTY3OA==$moKf/amkFlpGZrA9a//CrTtILZrEVsRFu2FIZfiLrrY=',0,N'Agent',2,0),
(4,N'agent03',N'agent03@b2b.local',N'pbkdf2$120000$QjJCRGVtb1NhbHQxMjM0NTY3OA==$moKf/amkFlpGZrA9a//CrTtILZrEVsRFu2FIZfiLrrY=',0,N'Agent',3,0),
(5,N'agent04',N'agent04@b2b.local',N'pbkdf2$120000$QjJCRGVtb1NhbHQxMjM0NTY3OA==$moKf/amkFlpGZrA9a//CrTtILZrEVsRFu2FIZfiLrrY=',0,N'Agent',4,0),
(6,N'agent05',N'agent05@b2b.local',N'pbkdf2$120000$QjJCRGVtb1NhbHQxMjM0NTY3OA==$moKf/amkFlpGZrA9a//CrTtILZrEVsRFu2FIZfiLrrY=',0,N'Agent',5,0),
(7,N'agent06',N'agent06@b2b.local',N'pbkdf2$120000$QjJCRGVtb1NhbHQxMjM0NTY3OA==$moKf/amkFlpGZrA9a//CrTtILZrEVsRFu2FIZfiLrrY=',0,N'Agent',6,0),
(8,N'agent07',N'agent07@b2b.local',N'pbkdf2$120000$QjJCRGVtb1NhbHQxMjM0NTY3OA==$moKf/amkFlpGZrA9a//CrTtILZrEVsRFu2FIZfiLrrY=',0,N'Agent',7,0),
(9,N'agent08',N'agent08@b2b.local',N'pbkdf2$120000$QjJCRGVtb1NhbHQxMjM0NTY3OA==$moKf/amkFlpGZrA9a//CrTtILZrEVsRFu2FIZfiLrrY=',0,N'Agent',8,0),
(10,N'agent09',N'agent09@b2b.local',N'pbkdf2$120000$QjJCRGVtb1NhbHQxMjM0NTY3OA==$moKf/amkFlpGZrA9a//CrTtILZrEVsRFu2FIZfiLrrY=',0,N'Agent',9,0),
(11,N'agent10',N'agent10@b2b.local',N'pbkdf2$120000$QjJCRGVtb1NhbHQxMjM0NTY3OA==$moKf/amkFlpGZrA9a//CrTtILZrEVsRFu2FIZfiLrrY=',0,N'Agent',10,0),
(12,N'agent11',N'agent11@b2b.local',N'pbkdf2$120000$QjJCRGVtb1NhbHQxMjM0NTY3OA==$moKf/amkFlpGZrA9a//CrTtILZrEVsRFu2FIZfiLrrY=',0,N'Agent',11,0),
(13,N'agent12',N'agent12@b2b.local',N'pbkdf2$120000$QjJCRGVtb1NhbHQxMjM0NTY3OA==$moKf/amkFlpGZrA9a//CrTtILZrEVsRFu2FIZfiLrrY=',0,N'Agent',12,0),
(14,N'agent13',N'agent13@b2b.local',N'pbkdf2$120000$QjJCRGVtb1NhbHQxMjM0NTY3OA==$moKf/amkFlpGZrA9a//CrTtILZrEVsRFu2FIZfiLrrY=',0,N'Agent',13,0),
(15,N'agent14',N'agent14@b2b.local',N'pbkdf2$120000$QjJCRGVtb1NhbHQxMjM0NTY3OA==$moKf/amkFlpGZrA9a//CrTtILZrEVsRFu2FIZfiLrrY=',0,N'Agent',14,0);
SET IDENTITY_INSERT dbo.Users OFF;
GO
SET IDENTITY_INSERT dbo.[Order] ON;
INSERT dbo.[Order](OrderID,OrderDate,AgentID,OrderStatus) VALUES
(1,'2026-10-01',1,N'Delivered'),
(2,'2026-10-03',2,N'Shipped'),
(3,'2026-10-05',3,N'Pending'),
(4,'2026-10-07',4,N'Confirmed'),
(5,'2026-10-09',5,N'Cancelled'),
(6,'2026-10-11',6,N'Delivered'),
(7,'2026-10-13',7,N'Pending'),
(8,'2026-10-15',1,N'Shipped'),
(9,'2026-10-17',2,N'Confirmed'),
(10,'2026-10-19',3,N'Delivered'),
(11,'2026-10-21',8,N'Pending'),
(12,'2026-10-23',9,N'Shipped'),
(13,'2026-10-25',10,N'Confirmed'),
(14,'2026-10-27',11,N'Delivered'),
(15,'2026-10-29',12,N'Pending'),
(16,'2026-10-31',13,N'Pending'),
(17,'2026-11-02',14,N'Confirmed'),
(18,'2026-11-04',15,N'Shipped'),
(19,'2026-11-06',1,N'Delivered'),
(20,'2026-11-08',2,N'Pending');
SET IDENTITY_INSERT dbo.[Order] OFF;
GO
SET IDENTITY_INSERT dbo.OrderDetail ON;
INSERT dbo.OrderDetail(ID,OrderID,ItemID,Quantity,UnitAmount) VALUES
(1,1,1,3,95000),(2,1,5,10,18000),(3,2,2,4,42000),(4,2,7,6,34000),(5,3,3,5,26000),
(6,3,4,2,32000),(7,4,6,2,115000),(8,4,8,3,56000),(9,5,9,10,12000),(10,5,10,4,25000),
(11,6,11,2,78000),(12,6,12,3,39000),(13,7,13,5,52000),(14,7,14,8,29000),(15,8,15,2,68000),
(16,8,1,1,95000),(17,9,2,3,42000),(18,9,3,4,26000),(19,10,4,6,32000),(20,10,5,12,18000),
(21,11,6,3,115000),
(22,12,7,10,34000),
(23,13,2,6,42000),
(24,14,9,20,12000),
(25,15,11,2,78000),
(26,16,13,4,52000),
(27,17,14,12,29000),
(28,18,3,8,26000),
(29,19,12,5,39000),
(30,20,15,3,68000);
SET IDENTITY_INSERT dbo.OrderDetail OFF;
GO

SELECT * FROM dbo.Item;
SELECT * FROM dbo.Agent;
SELECT UserID, UserName, Email, Role, AgentID, IsLocked, FailedLoginCount
FROM dbo.Users;