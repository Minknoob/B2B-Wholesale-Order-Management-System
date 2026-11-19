# Hướng dẫn chụp 14 ảnh bằng chứng cho phần M3 (`Screenshots_M3`)

Tài liệu này mô tả chính xác URL cần mở, tài khoản đăng nhập, dữ liệu đầu vào, câu lệnh SQL kiểm chứng trước/sau và vùng nội dung bắt buộc phải xuất hiện cho từng ảnh bằng chứng của **M3 (Web Core, Agent Login, Catalog & Master-Detail Order với Database Transaction)**.

> **Lưu ý chuẩn bị trước khi chụp:**
> 1. Khởi tạo cơ sở dữ liệu `B2BWholesale` trên SQL Server bằng `Database/CreateDatabase.sql` và `Database/SeedData.sql` (chỉ chạy trên DB mới, **không chạy trên DB đang có dữ liệu cần giữ**).
> 2. Cấu hình `ConnectionStrings:DefaultConnection` trong `B2B.AgentPortal/appsettings.json` (hoặc dùng User Secrets / biến môi trường nếu có mật khẩu SQL).
> 3. Khởi chạy ứng dụng web:
>    ```powershell
>    dotnet run --project B2B.AgentPortal/B2B.AgentPortal.csproj
>    ```
>    Mặc định ứng dụng lắng nghe tại `http://localhost:5180`.

---

## 1. `01_Agent_Login_Success.png` — Đăng nhập Agent thành công
- **Trang mở:** `http://localhost:5180/Account/Login`
- **Tài khoản nhập:**
  - Tên đăng nhập: `agent01`
  - Mật khẩu: `Demo@123`
- **Thao tác:** Nhấn **Đăng nhập vào Agent Portal**.
- **Nội dung bắt buộc có trong ảnh:**
  - Thanh địa chỉ trình duyệt đã chuyển sang `http://localhost:5180/` hoặc `http://localhost:5180/Catalog`.
  - Thanh điều hướng phía trên hiển thị: `Catalog`, `Create Order`, thông tin đại lý `An Phat Trading (agent01) · Mã ĐL #1` và nút `Đăng xuất`.
  - Bảng danh mục hàng hóa bán sỉ.

---

## 2. `02_Agent_Login_Wrong_Password.png` — Agent nhập sai mật khẩu
- **Trang mở:** `http://localhost:5180/Account/Login` (sau khi đã Đăng xuất).
- **Tài khoản nhập:**
  - Tên đăng nhập: `agent02`
  - Mật khẩu: `SaiMatKhau1` (nhập sai lần 1).
- **Truy vấn SQL kiểm tra kèm trong SSMS (nếu chụp song song):**
  ```sql
  SELECT UserID, UserName, Role, AgentID, IsLocked, FailedLoginCount
  FROM dbo.Users WHERE UserName = N'agent02';
  ```
- **Nội dung bắt buộc có trong ảnh:**
  - Hộp cảnh báo đỏ trên trang Login hiển thị thông báo an toàn: `"Tên đăng nhập hoặc mật khẩu không đúng."` (không lộ chi tiết username tồn tại hay không).
  - Trong SSMS (nếu ghép khung): `FailedLoginCount = 1`, `IsLocked = 0`.

---

## 3. `03_Admin_Web_Login_Denied.png` — Từ chối tài khoản Admin trên Web Portal
- **Trang mở:** `http://localhost:5180/Account/Login`
- **Tài khoản nhập:**
  - Tên đăng nhập: `admin`
  - Mật khẩu: `Demo@123`
- **Nội dung bắt buộc có trong ảnh:**
  - Trang Login từ chối đăng nhập và hiển thị rõ thông báo:
    `"Tài khoản này thuộc quyền Quản trị viên (Admin). Chỉ tài khoản Đại lý (Agent) mới được phép đăng nhập Agent Portal."`
  - Vẫn ở trang `/Account/Login`, không cấp cookie phiên Agent.

---

## 4. `04_Account_Locked_After_3_Failures.png` — Khóa tài khoản sau 3 lần nhập sai liên tiếp
- **Trang mở:** `http://localhost:5180/Account/Login`
- **Tài khoản thử nghiệm:** `agent14` (dùng tài khoản cuối để không ảnh hưởng `agent01`).
- **Thao tác:**
  1. Nhập `agent14` với mật khẩu sai `Wrong@1`, `Wrong@2`, `Wrong@3` đủ **3 lần liên tiếp**.
  2. Thử nhập lại lần thứ 4 bằng mật khẩu **đúng** `Demo@123` để xác nhận tài khoản đã khóa vẫn bị chặn.
- **Truy vấn SQL kiểm chứng trong SSMS:**
  ```sql
  SELECT UserID, UserName, IsLocked, FailedLoginCount
  FROM dbo.Users WHERE UserName = N'agent14';
  ```
- **Nội dung bắt buộc có trong ảnh:**
  - Thông báo khóa tài khoản màu đỏ trên giao diện đăng nhập (`IsLocked = true`, yêu cầu liên hệ Quản trị viên mở khóa).
  - Kết quả truy vấn SSMS hiển thị `IsLocked = 1`, `FailedLoginCount = 3`.
  - *(Sau khi chụp xong có thể mở khóa lại trong SSMS: `UPDATE dbo.Users SET IsLocked = 0, FailedLoginCount = 0 WHERE UserName = N'agent14';`)*

---

## 5. `05_Catalog.png` — Trang danh mục hàng hóa (Catalog)
- **Trang mở:** `http://localhost:5180/Catalog`
- **Tài khoản đăng nhập:** `agent01` / `Demo@123`
- **Nội dung bắt buộc có trong ảnh:**
  - Đầy đủ cột: `Mã SP`, `Tên mặt hàng`, `Quy cách (Size)`, `Đơn giá bán sỉ` (định dạng tiền tệ `₫`), `Tồn kho`, `Trạng thái` (`Còn hàng` / `Sắp hết` / `Hết hàng`) và ô số lượng + nút `Chọn đặt`.
  - Thanh công cụ lọc danh mục và nút `Tạo đơn nhiều mặt hàng`.

---

## 6. `06_Out_Of_Stock_Disabled.png` — Mặt hàng hết hàng bị khóa đặt
- **Chuẩn bị dữ liệu trong SSMS:**
  ```sql
  UPDATE dbo.Item SET StockQuantity = 0 WHERE ItemID = 11; -- Detergent (2 kg)
  ```
- **Trang mở:** `http://localhost:5180/Catalog` (nhấn F5 làm mới).
- **Nội dung bắt buộc có trong ảnh:**
  - Dòng `ItemID = 11` (`Detergent`) hiển thị `Tồn kho = 0`, huy hiệu đỏ `Hết hàng`.
  - Ô nhập số lượng và nút `Hết hàng` ở cột cuối bị vô hiệu hóa (`disabled`).
- *(Khôi phục sau khi chụp nếu cần: `UPDATE dbo.Item SET StockQuantity = 35 WHERE ItemID = 11;`)*

---

## 7. `07_Master_Detail_Order_Form.png` — Biểu mẫu đặt hàng Master-Detail nhiều dòng
- **Trang mở:** `http://localhost:5180/Orders/Create`
- **Tài khoản đăng nhập:** `agent01` / `Demo@123`
- **Dữ liệu nhập trên biểu mẫu:**
  - Nhấn `Thêm dòng sản phẩm` để tạo 3 dòng:
    - Dòng 1: `Rice 5kg — 5 kg` · Số lượng: `5`
    - Dòng 2: `Cooking Oil — 1 L` · Số lượng: `4`
    - Dòng 3: `Coffee — 500 g` · Số lượng: `2`
- **Nội dung bắt buộc có trong ảnh:**
  - Bảng chi tiết có nhiều dòng sản phẩm, nút `Thêm dòng sản phẩm` và nút `Xóa` từng dòng.
  - Cột `Đơn giá tham khảo`, `Thành tiền tham khảo` từng dòng và `Tổng tiền tham khảo` phía dưới tự động cập nhật (`873.000 ₫`).

---

## 8. `08_Order_Confirmation.png` — Trang xác nhận đơn hàng sau khi gửi thành công
- **Thao tác:** Từ bước 7, nhấn nút **Gửi đơn đặt hàng**.
- **Trang đích (POST–Redirect–GET):** `http://localhost:5180/Orders/Confirmation/{OrderID}`
- **Nội dung bắt buộc có trong ảnh:**
  - Thông báo xanh đặt hàng thành công.
  - Đầy đủ thông tin Master: `OrderID`, `OrderDate`, `OrderStatus = Pending`, thông tin Đại lý `An Phat Trading (#1)`.
  - Bảng Detail: danh sách mặt hàng, `Size`, `Quantity`, `UnitAmount` chốt từ DB, thành tiền từng dòng và `TotalAmount`.

---

## 9. `09_Stock_Before_Order.png` — Tồn kho trong SQL Server TRƯỚC khi đặt đơn
- **Truy vấn SQL chạy trong SSMS trước khi đặt đơn:**
  ```sql
  SELECT ItemID, ItemName, Size, Price, StockQuantity
  FROM dbo.Item
  WHERE ItemID IN (1, 2, 6);
  ```
- **Nội dung bắt buộc có trong ảnh:**
  - Cửa sổ SSMS hiển thị rõ câu truy vấn và bảng kết quả `StockQuantity` ban đầu của các mặt hàng sắp đặt (ví dụ: `ItemID = 1` có `StockQuantity = 120`, `ItemID = 2` có `StockQuantity = 85`, `ItemID = 6` có `StockQuantity = 45`).

---

## 10. `10_Stock_After_Order.png` — Tồn kho và dữ liệu đơn hàng SAU khi giao dịch Commit
- **Truy vấn SQL chạy trong SSMS ngay sau khi đặt đơn ở bước 8:**
  ```sql
  SELECT ItemID, ItemName, Size, Price, StockQuantity
  FROM dbo.Item
  WHERE ItemID IN (1, 2, 6);

  SELECT TOP 1 * FROM dbo.[Order] ORDER BY OrderID DESC;
  SELECT * FROM dbo.OrderDetail WHERE OrderID = (SELECT MAX(OrderID) FROM dbo.[Order]);
  ```
- **Nội dung bắt buộc có trong ảnh:**
  - `StockQuantity` đã giảm chính xác theo số lượng đặt (`120 -> 115`, `85 -> 81`, `45 -> 43`).
  - Dòng `Order` mới có `OrderStatus = Pending` và các dòng `OrderDetail` khớp `UnitAmount`.

---

## 11. `11_Insufficient_Stock_Blocked.png` — Chặn đặt hàng khi vượt tồn kho
- **Trang mở:** `http://localhost:5180/Orders/Create`
- **Dữ liệu nhập:**
  - Chọn `Oatmeal — 1 kg` (tồn kho hiện tại `40`), nhập số lượng `999` (hoặc thêm 2 dòng cùng chọn `Oatmeal` với số lượng `25` + `25` = `50 > 40`).
  - Nhấn **Gửi đơn đặt hàng**.
- **Nội dung bắt buộc có trong ảnh:**
  - Thông báo lỗi nghiệp vụ rõ ràng trên giao diện báo không đủ tồn kho và đơn hàng bị từ chối.

---

## 12. `12_Rollback_No_Order_Saved.png` — Bằng chứng Rollback toàn bộ khi 1 dòng hợp lệ + 1 dòng thiếu hàng
- **Thao tác:**
  1. Trong SSMS, kiểm tra trước:
     ```sql
     SELECT MAX(OrderID) AS MaxOrderBefore FROM dbo.[Order];
     SELECT ItemID, ItemName, StockQuantity FROM dbo.Item WHERE ItemID IN (1, 15);
     ```
  2. Trên `http://localhost:5180/Orders/Create`, tạo 2 dòng:
     - Dòng 1 (hợp lệ): `ItemID = 1` (`Rice 5kg`), `Quantity = 2` (đủ hàng).
     - Dòng 2 (thiếu hàng): `ItemID = 15` (`Oatmeal`), `Quantity = 500` (vượt tồn kho).
  3. Nhấn **Gửi đơn đặt hàng** -> hệ thống báo lỗi thiếu hàng.
  4. Chạy lại truy vấn SSMS kiểm tra sau:
     ```sql
     SELECT MAX(OrderID) AS MaxOrderAfter FROM dbo.[Order];
     SELECT ItemID, ItemName, StockQuantity FROM dbo.Item WHERE ItemID IN (1, 15);
     ```
- **Nội dung bắt buộc có trong ảnh:**
  - `MaxOrderBefore = MaxOrderAfter` (không có `Order` hay `OrderDetail` nào được lưu).
  - `StockQuantity` của **cả hai** mặt hàng `ItemID = 1` và `ItemID = 15` giữ nguyên 100%.

---

## 13. `13_Transaction_Code.png` — Đoạn mã nguồn Database Transaction & chống bán vượt kho
- **File mở trong IDE (Visual Studio / VS Code):** `B2B.AgentPortal/Services/OrderService.cs`
- **Nội dung bắt buộc có trong ảnh:**
  - Hàm `PlaceOrderRelationalTransactionAsync` thể hiện rõ:
    1. Mở giao dịch `BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken)`.
    2. Đọc giá từ DB, tạo `Order` (`OrderStatus = Pending`) + `OrderDetail` và `SaveChangesAsync`.
    3. Trừ kho nguyên tử có điều kiện `Where(i => i.ItemID == itemId && i.StockQuantity >= quantity).ExecuteUpdateAsync(...)` và kiểm tra `affectedRows != 1`.
    4. `CommitAsync` khi toàn bộ thành công và `RollbackAsync` trong mọi nhánh thất bại / `catch`.

---

## 14. `14_Appsettings_Secret_Hidden.png` — Cấu hình Connection String an toàn không chứa mật khẩu thật
- **File mở song song trong IDE:**
  - `B2B.AgentPortal/appsettings.json`
  - `B2B.AgentPortal/Program.cs` (đoạn `builder.Configuration.GetConnectionString("DefaultConnection")`)
- **Nội dung bắt buộc có trong ảnh:**
  - Chuỗi kết nối trong `appsettings.json` dùng Windows Authentication / placeholder (không chứa mật khẩu thật; nếu dùng SQL Auth local thì che phần `Password=***`).
  - Đoạn mã `Program.cs` đọc cấu hình qua `GetConnectionString("DefaultConnection")`, không hardcode chuỗi kết nối trong C#.
