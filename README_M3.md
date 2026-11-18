# B2B Wholesale Order Management System — Phần M3 (Agent Web Portal Core & Database Transaction)

Tài liệu bàn giao chính thức cho **Phần M3 (Task 3.1 & Task 3.2)**: Cổng đặt hàng trực tuyến dành cho Đại lý (`B2B.AgentPortal`), đăng nhập phân quyền dùng chung `B2B.Core`, danh mục sản phẩm (Catalog) và đặt hàng nhiều dòng (Master-Detail Order) xử lý nguyên tử trong một giao dịch cơ sở dữ liệu (Database Transaction).

> Tài liệu gốc của M1 tại `README.md` được giữ nguyên hoàn toàn.

---

## 1. Công nghệ & Kiến trúc sử dụng

- **Nền tảng:** .NET 8 (`net8.0`)
- **Web Framework:** ASP.NET Core MVC (Option C — Code First)
- **ORM & Truy cập dữ liệu:** Entity Framework Core 8 (`Microsoft.EntityFrameworkCore.SqlServer` `8.0.11`)
- **Cơ sở dữ liệu:** Microsoft SQL Server (`B2BWholesale`)
- **Xác thực & Bảo mật:** ASP.NET Core Cookie Authentication (`HttpOnly`, `SameSite=Lax`, chống Open Redirect, `[ValidateAntiForgeryToken]` cho mọi POST), dùng chung thư viện `B2B.Core` (`PasswordHasher` PBKDF2-SHA256 120.000 vòng lặp & `AccountLockout` khóa sau 3 lần sai).
- **Giao diện tối thiểu:** Bootstrap 5 (tích hợp sẵn offline trong `wwwroot/lib/bootstrap`), hỗ trợ responsive trên Desktop và Mobile.

---

## 2. Cấu hình Connection String an toàn

Chuỗi kết nối được đọc qua `builder.Configuration.GetConnectionString("DefaultConnection")` trong `B2B.AgentPortal/Program.cs` từ `B2B.AgentPortal/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=B2BWholesale;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

- Nếu máy của bạn dùng **SQL Server Express**, đổi `Server=localhost` thành `Server=.\SQLEXPRESS`.
- **Không commit mật khẩu thật lên GitHub:** Nếu dùng SQL Authentication (`User Id=...;Password=...`), hãy cấu hình bằng **.NET User Secrets** hoặc **Environment Variables** thay vì ghi trực tiếp vào `appsettings.json`:
  ```powershell
  dotnet user-secrets init --project B2B.AgentPortal/B2B.AgentPortal.csproj
  dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=YOUR_SERVER;Database=B2BWholesale;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;" --project B2B.AgentPortal/B2B.AgentPortal.csproj
  ```

---

## 3. Khởi tạo Cơ sở dữ liệu (2 kịch bản hỗ trợ)

> **CẢNH BÁO QUAN TRỌNG:**
> Tập tin `Database/CreateDatabase.sql` của M1 có các lệnh `DROP TABLE` để làm sạch bảng cũ. **Tuyệt đối không chạy lại `CreateDatabase.sql` trên cơ sở dữ liệu đang chứa dữ liệu cần giữ.**
> Ứng dụng `B2B.AgentPortal` **không** tự động gọi `Database.EnsureCreated()` hay `Database.Migrate()` khi khởi động để bảo vệ toàn vẹn dữ liệu.

### Cách A: Cơ sở dữ liệu ĐÃ ĐƯỢC tạo bằng SQL Script của M1 (Khuyến nghị bắt buộc khi chạy chung với M1/M2)
1. Nếu chưa tạo DB, mở SSMS chạy theo thứ tự:
   - `Database/CreateDatabase.sql`
   - `Database/SeedData.sql`
   - `Database/TestIntegrity.sql`
2. **Tuyệt đối không chạy `dotnet ef database update` lên database M1 hiện tại.** Giữ nguyên schema hiện có, chỉ cấu hình `ConnectionStrings:DefaultConnection` và chạy trực tiếp `B2B.AgentPortal`. `B2BDbContext` đã ánh xạ đúng tên bảng (`dbo.Item`, `dbo.Agent`, `dbo.[Order]`, `dbo.OrderDetail`, `dbo.Users`), kiểu dữ liệu cột, ràng buộc `CHECK`/`UNIQUE` và quy tắc xóa khóa ngoại (`ON DELETE CASCADE` cho `OrderDetail -> Order`, `ON DELETE NO ACTION` cho các bảng còn lại).

### Cách B: Cơ sở dữ liệu SẠCH hoàn toàn (Chỉ dùng khi tạo một database hoàn toàn mới từ đầu)
Chỉ áp dụng khi tạo một database mới hoàn toàn chưa từng chạy `CreateDatabase.sql`:
```powershell
dotnet ef database update --project B2B.AgentPortal --startup-project B2B.AgentPortal
```
Sau khi tạo bảng bằng Migration, có thể nạp dữ liệu mẫu bằng `Database/SeedData.sql`.
*(Lưu ý kỹ thuật: Migration của EF Core tạo đầy đủ bảng, kiểu dữ liệu, `CHECK`, `UNIQUE` và `ON DELETE`, riêng mệnh đề `ON UPDATE CASCADE` trong script SQL gốc của M1 không sinh tự động trong EF Core Migration — điều này không ảnh hưởng nghiệp vụ thực tế vì toàn bộ khóa chính đều là cột `INT IDENTITY` không bao giờ cập nhật giá trị).*

---

## 4. Cách Build và Chạy Agent Portal

Tại thư mục gốc chứa `B2B-M1.sln`:

```powershell
dotnet restore B2B-M1.sln
dotnet build B2B-M1.sln
dotnet run --project B2B.AgentPortal/B2B.AgentPortal.csproj
```

Các đường dẫn (URL) chính của M3 (mặc định `http://localhost:5180`):

| Chức năng | Phương thức & Route | Ghi chú phân quyền |
|---|---|---|
| Trang chủ mặc định | `GET /` | Chuyển về Catalog (chưa đăng nhập sẽ về `/Account/Login`) |
| Đăng nhập Agent | `GET /Account/Login`, `POST /Account/Login` | `[AllowAnonymous]`, chống Open Redirect |
| Đăng xuất | `POST /Account/Logout` | Yêu cầu Anti-Forgery Token (không dùng `GET`) |
| Từ chối truy cập | `GET /Account/AccessDenied` | Trả mã `403 Forbidden` |
| Danh mục hàng hóa | `GET /Catalog`, `GET /Catalog/Index` | `[Authorize(Roles = "Agent")]` |
| Đặt hàng Master-Detail | `GET /Orders/Create`, `POST /Orders/Create` | `[Authorize(Roles = "Agent")]`, POST–Redirect–GET |
| Xác nhận đơn vừa đặt | `GET /Orders/Confirmation/{id}` | Kiểm tra quyền sở hữu theo `AgentID` trong claim |

---

## 5. Tài khoản Demo & Quy tắc Khóa Tài khoản

Từ dữ liệu seed chuẩn của M1 (`Database/SeedData.sql`):
- **Tài khoản Đại lý (được phép vào Web Portal):** `agent01` đến `agent14` (Mật khẩu mẫu của seed data: xem mục 3 trong `README.md` của M1).
- **Tài khoản Quản trị (bị từ chối trên Web Portal):** `admin` (chỉ được phép đăng nhập ứng dụng WinForms).

**Quy tắc bảo mật tài khoản (`B2B.Core`):**
- Băm mật khẩu bằng **PBKDF2-SHA256** (`120,000` iterations).
- Nhập sai mật khẩu tăng `FailedLoginCount`; sai đủ **3 lần liên tiếp** sẽ đặt `IsLocked = 1` (`true`).
- Tài khoản đã khóa (`IsLocked = 1`) bị từ chối đăng nhập ngay cả khi nhập đúng mật khẩu và **không tự mở khóa**.
- Đăng nhập thành công đặt `FailedLoginCount = 0`.
- Quản trị viên mở khóa qua nghiệp vụ quản trị hoặc SSMS theo hướng dẫn tại `README.md` mục 6.

---

## 6. Hướng dẫn Tích hợp cho Thành viên M4 (My Orders, Printable Invoice & UI)

M3 đã chuẩn bị sẵn đầy đủ nền tảng dùng chung để M4 mở rộng Task 3.3 và Task 3.4 mà không cần sửa lại lõi xác thực hay DbContext:

1. **Tên project web:** `B2B.AgentPortal` (`B2B.AgentPortal/B2B.AgentPortal.csproj`)
2. **Routes đã có:**
   - Login: `/Account/Login`
   - Catalog: `/Catalog` (`/Catalog/Index`)
   - Tạo đơn: `/Orders/Create`
   - Xác nhận đơn: `/Orders/Confirmation/{id}`
3. **Hằng số Claim & lấy `AgentID` hiện tại:**
   - File: `B2B.AgentPortal/Security/AgentPortalClaims.cs`
   - Tên claim chuẩn: `AgentPortalClaims.AgentIdClaimType` (`"AgentID"`)
   - Extension lấy `AgentID` an toàn trong Controller / View:
     ```csharp
     using B2B.AgentPortal.Security;

     if (!User.TryGetAgentId(out int currentAgentId))
     {
         return Challenge(); // hoặc yêu cầu đăng nhập lại
     }
     ```
4. **Entities & Tính `TotalAmount`:**
   - `Order` (`B2B.AgentPortal/Models/Order.cs`) và `OrderDetail` (`B2B.AgentPortal/Models/OrderDetail.cs`).
   - Bảng `dbo.[Order]` không lưu cột `TotalAmount` dư thừa; dùng phương thức có sẵn:
     - Trên entity đã `Include(o => o.OrderDetails)`: `decimal total = order.CalculateTotalAmount();`
     - Trong truy vấn LINQ to Entities: `o.OrderDetails.Sum(d => d.Quantity * d.UnitAmount)`
5. **Danh sách `OrderStatus` hợp lệ:**
   - Dùng hằng số tại `B2B.AgentPortal/Models/OrderStatuses.cs`:
     `Pending`, `Confirmed`, `Shipped`, `Delivered`, `Cancelled` (`OrderStatuses.All`).
6. **Kiểm tra quyền sở hữu đơn hàng (Chống IDOR cho trang My Orders & Invoice):**
   - Dùng `order.BelongsToAgent(currentAgentId)` hoặc gọi trực tiếp `await _orderService.GetOrderForAgentAsync(orderId, currentAgentId, cancellationToken)`:
     - `OrderAccessStatus.NotFound` -> trả `NotFound()` (`404`)
     - `OrderAccessStatus.Forbidden` -> trả `403 Forbidden` (không tiết lộ dữ liệu đơn của Agent khác).

---

## 7. Hướng dẫn Viết Unit Test cho Thành viên M5

Toàn bộ nghiệp vụ đăng nhập và đặt hàng đã được tách hoàn toàn khỏi Controller sang tầng Service để M5 viết Unit Test dễ dàng:

1. **Tham chiếu cần thêm vào project Test của M5:**
   - `ProjectReference` tới `B2B.Core/B2B.Core.csproj` và `B2B.AgentPortal/B2B.AgentPortal.csproj`.
2. **Các thành phần cần test chính:**
   - `B2B.Core.AccountLockout` & `B2B.Core.PasswordHasher`
   - `B2B.AgentPortal.Services.IOrderService` / `OrderService`:
     - Phương thức: `PlaceOrderAsync(int agentId, IReadOnlyCollection<OrderLineRequest> lines, CancellationToken cancellationToken = default)`
     - DTO đầu vào: `OrderLineRequest(int ItemId, int Quantity)`
     - Kết quả trả về: `PlaceOrderResult` (`Success`, `OrderId`, `OrderDate`, `Status`, `Lines`, `TotalAmount`, `ErrorCode`, `ErrorMessage`)
     - Mã lỗi nghiệp vụ (`OrderErrorCode`): `EmptyOrder`, `InvalidQuantity`, `InvalidItem`, `InsufficientStock`, `InvalidAgent`, `ConcurrencyConflict`, `UnexpectedFailure`.
   - `B2B.AgentPortal.Services.IAgentAuthenticationService` / `AgentAuthenticationService`
3. **Cách khởi tạo `B2BDbContext` giả lập trong Unit Test:**
   - `OrderService` hỗ trợ **cả 3 chế độ provider**:
     1. **EF Core InMemory (`UseInMemoryDatabase`)**: chạy nhanh cho unit test logic cộng gộp dòng, kiểm tra tồn kho, tính `TotalAmount`, chặn `Quantity <= 0`.
     2. **SQLite In-Memory (`UseSqlite("DataSource=:memory:")`)** hoặc **SQL Server Test DB** (đọc chuỗi kết nối từ file cấu hình test, không hardcode): kiểm tra đầy đủ `BeginTransactionAsync`, `ExecuteUpdateAsync` và `RollbackAsync`.
   - `OrderService` có thêm constructor nhận `beforeCommitHook` (`Func<CancellationToken, Task>?`) để test giả lập lỗi ngay trước `CommitAsync` (kiểm chứng rollback 100%).

---

## 8. Quy trình Đưa thay đổi lên Nhánh `feature/m3-web-order` (Git Workflow)

Nếu thư mục làm việc hiện tại được giải nén từ file ZIP chưa có `.git`, thực hiện các bước sau để đưa code lên đúng nhánh `feature/m3-web-order` (**tuyệt đối không push trực tiếp lên `main`**):

```powershell
# 1. Clone repository chính thức và tạo nhánh feature/m3-web-order từ nhánh nền của nhóm
git clone https://github.com/Minknoob/B2B-Wholesale-Order-Management-System.git b2b-repo
cd b2b-repo
git fetch origin
git checkout -b feature/m3-web-order origin/feature/m1-db-login

# 2. Sao chép các phần M3 đã triển khai sang thư mục repo (KHÔNG chép bin/, obj/, .vs/)
#    - Thư mục B2B.AgentPortal/ (bỏ qua bin/ và obj/)
#    - Thư mục Screenshots_M3/
#    - File B2B-M1.sln, README_M3.md, M3_TRANSACTION_TESTS.md

# 3. Kiểm tra trạng thái & git diff trước khi commit
git status
git diff B2B-M1.sln

# 4. Đảm bảo không có bin/, obj/, .vs/ hoặc mật khẩu thật được stage
git add B2B-M1.sln B2B.AgentPortal README_M3.md M3_TRANSACTION_TESTS.md Screenshots_M3
git commit -m "feat(m3): implement Agent Portal login, catalog, and transactional master-detail ordering"

# 5. Đẩy lên nhánh feature/m3-web-order và tạo Pull Request để thành viên khác review
git push -u origin feature/m3-web-order
```

---

## 9. Giới hạn Phạm vi M3 (Theo phân công nhóm)

- Chưa bao gồm trang **My Orders** và **Printable Invoice** chính thức (thuộc phạm vi Task 3.3 của **M4**).
- Giao diện hiện ở mức chuẩn sạch, rõ ràng và responsive với Bootstrap 5; phần nâng cấp DataTables / bộ icon hoàn chỉnh toàn trang thuộc Task 3.4 của **M4**.
- Không tạo project Unit Test chính thức trong solution để tránh xung đột với phần **M5** (Task 5.1).
