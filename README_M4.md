# M4: Web UI, My Orders, Printable Invoice & Git Integration

## 1. Các thành phần đã hoàn thiện (tích hợp trực tiếp trên nền M1 & M3)

- **My Orders (`GET /Orders` — `OrdersController.MyOrders.cs`, `Views/Orders/Index.cshtml`):**
  - Chỉ truy vấn và hiển thị các đơn hàng có `Order.AgentID == currentAgentId` lấy từ claim `AgentID` của đại lý đang đăng nhập (`User.TryGetAgentId(out int agentId)`).
  - Hiển thị mã đơn (`#OrderID`), ngày đặt, trạng thái (`Pending`, `Confirmed`, `Shipped`, `Delivered`, `Cancelled`), số dòng mặt hàng và tổng tiền (`Sum(Quantity * UnitAmount)`).
  - Tích hợp **DataTables** (tìm kiếm, sắp xếp theo ngày/giá trị, phân trang) và **Bootstrap Icons**.

- **Order Details & Phân quyền sở hữu phía Server (`GET /Orders/Details/{id}` — `Views/Orders/Details.cshtml`):**
  - Kiểm tra đồng thời `o.OrderID == orderId && o.AgentID == agentId` trong `LoadOwnedOrderAsync`.
  - Nếu Agent A cố tình nhập URL `/Orders/Details/{id}` của đơn thuộc Agent B, controller trả về `404 Not Found` (không tiết lộ sự tồn tại của đơn hàng).

- **In hóa đơn (`GET /Orders/Invoice/{id}` — `Views/Orders/Invoice.cshtml`, `wwwroot/css/site.css`):**
  - Kiểm tra quyền sở hữu tương tự trang chi tiết (`404 Not Found` nếu truy cập đơn của đại lý khác).
  - Layout riêng không navbar, có thanh công cụ `.no-print` và CSS `@media print` chuẩn khổ A4 (ẩn nút điều khiển khi in, giữ lặp tiêu đề bảng và tránh ngắt dòng giữa chừng).

- **Layout & Responsive (`Views/Shared/_Layout.cshtml`):**
  - Navbar Bootstrap 5 thay đổi trạng thái theo đăng nhập (ẩn Catalog/Tạo đơn/My Orders khi chưa đăng nhập; hiển thị đầy đủ menu, thông tin Đại lý + `#AgentID` và nút Đăng xuất POST có AntiForgeryToken khi đã đăng nhập).
  - Bố cục tương thích cả màn hình rộng (Desktop) và điện thoại (Mobile `<= 576px`).
