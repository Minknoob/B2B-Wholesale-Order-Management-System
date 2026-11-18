B2B WHOLESALE - PHAN M1 (DATABASE, WINFORMS LOGIN, CONFIG)
===========================================================

1. YEU CAU
- Windows 10/11
- SQL Server 2019+ (hoac SQL Server Express) va SSMS
- Visual Studio 2022 voi workload .NET desktop development
- .NET 8 SDK

2. TAO DATABASE
Buoc 1: Mo SQL Server Management Studio va ket noi den SQL Server cua ban.
Buoc 2: Mo Database/CreateDatabase.sql va Execute.
Buoc 3: Mo Database/SeedData.sql va Execute.
Buoc 4: Mo Database/TestIntegrity.sql va Execute. Cac dong PASS cho biet constraint hoat dong.
Luu y: CreateDatabase.sql se DROP cac bang cu va tao lai schema. Khong chay lai tren database dang co du lieu can giu.

3. TAI KHOAN DEMO
Admin:
  Username: admin
  Password: Demo@123
Agent de kiem tra bi tu choi:
  Username: agent01
  Password: Demo@123
Co 1 Admin va 14 Agent (agent01..agent14), tat ca dung mat khau Demo@123.
Mat khau duoc luu duoi dang PBKDF2-SHA256 hash, khong luu plaintext.

4. CAU HINH KET NOI WINFORMS
Mo B2B.WinForms/App.config va sua Server=localhost thanh ten SQL Server tren may ban.
Vi du:
  Server=.\SQLEXPRESS;Database=B2BWholesale;Trusted_Connection=True;TrustServerCertificate=True;
Hoac dung SQL Authentication:
  Server=localhost;Database=B2BWholesale;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;
Khong commit mat khau that len GitHub. Neu dung SQL Authentication, giu file config local va khong dua credential that vao commit.

5. CHAY WINFORMS
- Mo file B2B.WinForms.csproj bang Visual Studio 2022.
- Restore NuGet packages.
- Set B2B.WinForms lam Startup Project.
- Run (F5).
- Dang nhap bang admin / Demo@123.
- Thu dang nhap agent01 / Demo@123: he thong phai tu choi vi day la tai khoan Agent.
- Thu nhap sai mat khau admin 3 lan: tai khoan se bi khoa.

6. MO KHOA TAI KHOAN (ADMIN THAO TAC TRONG SSMS)
UPDATE dbo.Users SET IsLocked = 0, FailedLoginCount = 0 WHERE UserName = N'admin';
Thay admin bang username can mo khoa. Chi thuc hien boi nguoi co quyen quan tri Database.

7. QUY UOC DATABASE / LY DO FOREIGN KEY
- Order.AgentID -> Agent.AgentID: ON DELETE NO ACTION de khong xoa dai ly da co lich su don hang; ON UPDATE CASCADE.
- OrderDetail.OrderID -> Order.OrderID: ON DELETE CASCADE vi dong chi tiet khong ton tai doc lap khi don hang bi xoa.
- OrderDetail.ItemID -> Item.ItemID: ON DELETE NO ACTION de chan xoa mat hang da duoc su dung trong don.
- Users.AgentID -> Agent.AgentID: ON DELETE NO ACTION de bao toan lien ket tai khoan Agent voi dai ly.

8. ERD (QUAN HE)
Item (1) -------- (n) OrderDetail (n) -------- (1) Order (n) -------- (1) Agent
Agent (1) ------- (n) Users [AgentID nullable; Admin de NULL]
