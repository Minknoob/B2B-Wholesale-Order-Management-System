# Kịch bản & Kết quả Kiểm thử Giao dịch Đặt hàng Thực tế (`M3_TRANSACTION_TESTS.md`)

Tài liệu này ghi nhận kết quả kiểm thử thực tế **15 kịch bản Database Transaction & Chống bán vượt tồn kho** của `OrderService.PlaceOrderAsync` trên **Microsoft SQL Server (`.\SQLEXPRESS`, Database `B2BWholesale` khởi tạo từ `Database/CreateDatabase.sql` + `Database/SeedData.sql` của M1)**.

- **Ngày thực thi kiểm thử:** `2026-11-18`
- **Script chạy tự động tái lập kết quả:** `Screenshots_M3/RunM3TransactionTests.ps1` và script kiểm tra SQL trực tiếp `Database/TestM3Transactions.sql`
- **Log đầu ra thực tế đã lưu:** `Screenshots_M3/M3_TRANSACTION_TEST_LOG.txt`
- **Ảnh bằng chứng thực tế:** `Screenshots_M3/01_Agent_Login_Success.png` đến `Screenshots_M3/14_Appsettings_Secret_Hidden.png`
- **Cơ chế giao dịch áp dụng:**
  - `IsolationLevel.ReadCommitted` kết hợp khóa dòng nguyên tử bằng câu lệnh cập nhật tham số hóa có điều kiện:
    ```csharp
    int affectedRows = await _dbContext.Items
        .Where(i => i.ItemID == itemId && i.StockQuantity >= quantity)
        .ExecuteUpdateAsync(
            setters => setters.SetProperty(i => i.StockQuantity, i => i.StockQuantity - quantity),
            cancellationToken);
    ```
  - Các dòng trùng `ItemID` được gộp trước bằng phép cộng có kiểm tra tràn số (`checked`), sắp xếp tăng dần theo `ItemID` để loại trừ deadlock chéo (`AB-BA`), và kiểm tra `affectedRows == 1` cho từng mặt hàng trong cùng một `IDbContextTransaction`. Nếu bất kỳ bước nào thất bại, toàn bộ `Order`, `OrderDetail` và các lần trừ `StockQuantity` trước đó đều bị `RollbackAsync()`.

---

## Bảng tổng hợp 15 kịch bản kiểm thử đã chạy trên SQL Server

| Mã kịch bản | Kịch bản kiểm thử | Dữ liệu đầu vào | Stock trước | Kết quả mong đợi | Order lưu? | OrderDetail lưu? | Stock sau | Kết quả thực tế (đã chạy trên SQL Server) | Pass / Fail | Vị trí ảnh bằng chứng |
|---|---|---|---|---|---|---|---|---|---|---|
| **TC-01** | Một Item, tồn kho đủ | `AgentID = 1`, `[ItemID=1 (Rice 5kg, 95,000₫), Qty=5]` | `Item #1 = 120` (Tổng Order=20, Detail=30) | Tạo `Order` (`Pending`), lưu 1 `OrderDetail`, trừ kho 5 đơn vị, `TotalAmount = 475,000` | **Có** (`OrderID = 21`) | **Có** (1 dòng, tổng 31) | `Item #1 = 115` | `Success = True`, `OrderID = 21`, `OrderStatus = Pending`, `TotalAmount = 475,000.00`, `Stock = 115` | **PASS** | `Screenshots_M3/08_Order_Confirmation.png`, `09_Stock_Before_Order.png`, `10_Stock_After_Order.png` |
| **TC-02** | Nhiều Item, tất cả tồn kho đủ | `AgentID = 1`, `[ItemID=2 (42,000₫), Qty=4; ItemID=6 (115,000₫), Qty=2]` | `Item #2 = 85`, `Item #6 = 45` (Order=21, Detail=31) | Lưu 1 `Order`, 2 `OrderDetail`, trừ kho cả 2 mặt hàng, `TotalAmount = 398,000` | **Có** (`OrderID = 22`) | **Có** (2 dòng, tổng 33) | `Item #2 = 81`, `Item #6 = 43` | `Success = True`, `OrderID = 22`, `TotalAmount = 398,000.00`, cả 2 tồn kho giảm chính xác | **PASS** | `Screenshots_M3/07_Master_Detail_Order_Form.png`, `08_Order_Confirmation.png` |
| **TC-03** | `Quantity` đúng bằng `StockQuantity` | `AgentID = 2`, `[ItemID=11 (Detergent, 78,000₫), Qty=35]` | `Item #11 = 35` (Order=22, Detail=33) | Đặt thành công vét hết kho, `StockQuantity` về đúng `0`, `TotalAmount = 2,730,000` | **Có** (`OrderID = 23`) | **Có** (1 dòng, tổng 34) | `Item #11 = 0` | `Success = True`, `OrderID = 23`, `TotalAmount = 2,730,000.00`, `Stock #11 = 0` (trở thành hết hàng) | **PASS** | `Screenshots_M3/06_Out_Of_Stock_Disabled.png` |
| **TC-04** | `Quantity` lớn hơn `StockQuantity` | `AgentID = 2`, `[ItemID=15 (Oatmeal), Qty=45]` | `Item #15 = 40` (Order=23, Detail=34) | Chặn đặt hàng (`InsufficientStock`), rollback, không lưu gì | **Không** (vẫn 23) | **Không** (vẫn 34) | `Item #15 = 40` | `Success = False`, `ErrorCode = InsufficientStock`, tổng Order=23, Detail=34, `Stock #15 = 40` | **PASS** | `Screenshots_M3/11_Insufficient_Stock_Blocked.png` |
| **TC-05** | `Quantity = 0` | `AgentID = 2`, `[ItemID=3 (Sugar), Qty=0]` | `Item #3 = 70` (Order=23, Detail=34) | Từ chối ngay ở tầng nghiệp vụ với `InvalidQuantity` | **Không** (vẫn 23) | **Không** (vẫn 34) | `Item #3 = 70` | `Success = False`, `ErrorCode = InvalidQuantity`, không chạm DB tồn kho | **PASS** | `Screenshots_M3/07_Master_Detail_Order_Form.png` |
| **TC-06** | `Quantity` âm (`-5`) | `AgentID = 2`, `[ItemID=3 (Sugar), Qty=-5]` | `Item #3 = 70` (Order=23, Detail=34) | Từ chối ngay ở tầng nghiệp vụ với `InvalidQuantity` | **Không** (vẫn 23) | **Không** (vẫn 34) | `Item #3 = 70` | `Success = False`, `ErrorCode = InvalidQuantity`, không có dòng nào được ghi | **PASS** | `Screenshots_M3/07_Master_Detail_Order_Form.png` |
| **TC-07** | Một Item lặp 2 dòng, tổng vẫn đủ tồn kho | `AgentID = 3`, `[ItemID=3 (26,000₫), Qty=10; ItemID=3, Qty=15]` (Tổng = 25) | `Item #3 = 70` (Order=23, Detail=34) | Gộp thành 1 dòng `Qty = 25`, trừ 25 đơn vị tồn kho, `TotalAmount = 650,000` | **Có** (`OrderID = 24`) | **Có** (gộp thành 1 dòng, tổng 35) | `Item #3 = 45` | `Success = True`, `OrderID = 24`, `MergedLines = 1`, `TotalAmount = 650,000.00`, `Stock #3 = 45` | **PASS** | `Screenshots_M3/07_Master_Detail_Order_Form.png`, `08_Order_Confirmation.png` |
| **TC-08** | Một Item lặp 2 dòng, từng dòng đủ nhưng tổng vượt tồn kho | `AgentID = 3`, `[ItemID=3, Qty=30; ItemID=3, Qty=25]` (Từng dòng `< 45`, nhưng tổng `55 > 45`) | `Item #3 = 45` (Order=24, Detail=35) | Kiểm tra theo tổng gộp (`55 > 45`), chặn `InsufficientStock`, không lưu gì | **Không** (vẫn 24) | **Không** (vẫn 35) | `Item #3 = 45` | `Success = False`, `ErrorCode = InsufficientStock`, Order=24, Detail=35, `Stock #3 = 45` | **PASS** | `Screenshots_M3/11_Insufficient_Stock_Blocked.png` |
| **TC-09** | Một `ItemID` không tồn tại | `AgentID = 3`, `[ItemID=4, Qty=2; ItemID=99999, Qty=1]` | `Item #4 = 95` (Order=24, Detail=35) | Trả `InvalidItem`, rollback toàn bộ, `Item #4` không bị trừ kho | **Không** (vẫn 24) | **Không** (vẫn 35) | `Item #4 = 95` | `Success = False`, `ErrorCode = InvalidItem`, Order=24, Detail=35, `Stock #4 = 95` | **PASS** | `Screenshots_M3/12_Rollback_No_Order_Saved.png` |
| **TC-10** | 1 dòng hợp lệ + 1 dòng thiếu hàng: Rollback toàn bộ | `AgentID = 4`, `[ItemID=4, Qty=5 (đủ); ItemID=15, Qty=100 (thiếu)]` | `Item #4 = 95`, `Item #15 = 40` (Order=24, Detail=35) | Rollback toàn bộ giao dịch: không lưu `Order`, không lưu `OrderDetail`, cả 2 Item giữ nguyên tồn kho | **Không** (vẫn 24) | **Không** (vẫn 35) | `Item #4 = 95`, `Item #15 = 40` | `Success = False`, `ErrorCode = InsufficientStock`, Order=24, Detail=35, cả 2 Item nguyên vẹn | **PASS** | `Screenshots_M3/12_Rollback_No_Order_Saved.png` |
| **TC-11** | Lỗi giả lập SAU khi lưu `Order` + `OrderDetail` và trừ kho nhưng TRƯỚC `Commit` | `AgentID = 4`, `[ItemID=5 (Instant Noodles), Qty=12]` + `beforeCommitHook` ném ngoại lệ | `Item #5 = 200` (Order=24, Detail=35) | Ngoại lệ bắt trong `catch`, gọi `RollbackAsync()`, thu hồi toàn bộ `INSERT Order`, `INSERT OrderDetail` và `UPDATE Item` | **Không** (vẫn 24) | **Không** (vẫn 35) | `Item #5 = 200` | `Success = False`, `ErrorCode = UnexpectedFailure`, Order=24, Detail=35, `Stock #5 = 200` (hoàn tác 100%) | **PASS** | `Screenshots_M3/12_Rollback_No_Order_Saved.png`, `13_Transaction_Code.png` |
| **TC-12** | Hai request đồng thời mua số lượng lớn của cùng 1 Item (Concurrency / Race Condition) | `AgentID = 5` đặt `[ItemID=15, Qty=30]` **đồng thời cùng lúc** với `AgentID = 6` đặt `[ItemID=15, Qty=30]` | `Item #15 = 40` (Order=24, Detail=35) | Đúng **1 request thành công** (`Stock` giảm `40 -> 10`), request còn lại bị chặn `InsufficientStock` và rollback; tuyệt đối không âm kho | **Đúng 1 đơn** (tổng 24 -> 25) | **Đúng 1 dòng** (tổng 35 -> 36) | `Item #15 = 10` | `SuccessCount = 1`, `FailCount = 1` (`InsufficientStock`), `Stock #15 = 10`, không bán vượt tồn kho | **PASS** | `Screenshots_M3/13_Transaction_Code.png` |
| **TC-13** | Giá `Item` thay đổi sau khi mở form nhưng trước khi bấm gửi | Form mở lúc `Item #8 (Tea)` giá `56,000₫`; DB cập nhật `Price = 61,000₫` trước khi `PlaceOrderAsync(AgentID=7, [ItemID=8, Qty=3])` | `Item #8 = 60` (Order=25, Detail=36) | Server bỏ qua giá phía client, đọc giá mới `61,000₫` từ DB -> `UnitAmount = 61,000`, `TotalAmount = 183,000` | **Có** (`OrderID = 27`) | **Có** (1 dòng, tổng 37) | `Item #8 = 57` | `Success = True`, `UnitAmount = 61,000.00`, `TotalAmount = 183,000.00`, `Stock #8 = 57` | **PASS** | `Screenshots_M3/08_Order_Confirmation.png` |
| **TC-14** | `AgentID` không tồn tại (`9999`) hoặc claim không hợp lệ (`0`) | `PlaceOrderAsync(9999, [ItemID=7, Qty=2])` và `PlaceOrderAsync(0, [ItemID=7, Qty=2])` | `Item #7 = 110` (Order=26, Detail=37) | Trả `InvalidAgent`, trên Web Controller buộc đăng xuất và yêu cầu đăng nhập lại | **Không** (vẫn 26) | **Không** (vẫn 37) | `Item #7 = 110` | Cả 2 trường hợp trả `ErrorCode = InvalidAgent`, Order=26, Detail=37, `Stock #7 = 110` | **PASS** | `Screenshots_M3/01_Agent_Login_Success.png` |
| **TC-15** | Danh sách dòng đặt hàng rỗng (`Empty`) | `AgentID = 1`, `lines = []` | Không đổi (Order=26, Detail=37) | Trả `EmptyOrder`, không mở giao dịch thừa, không lưu dữ liệu | **Không** (vẫn 26) | **Không** (vẫn 37) | Không đổi | `Success = False`, `ErrorCode = EmptyOrder`, Order=26, Detail=37 | **PASS** | `Screenshots_M3/07_Master_Detail_Order_Form.png` |

---

## Giải thích kỹ thuật: Vì sao cơ chế của M3 chống Lost Update & Overselling triệt để?

1. **Vấn đề của cách đọc – so sánh – gán thông thường:**
   Nếu hai request `T1` và `T2` cùng đọc `Item #15` (`StockQuantity = 40`) bằng EF Core thông thường, cả hai đều thấy `40 >= 30`, cùng gán `item.StockQuantity = 10` trong bộ nhớ rồi `SaveChanges()` -> cả 2 đơn (tổng 60 đơn vị) đều thành công trong khi kho chỉ có 40 (mất cập nhật — *Lost Update* và bán lố 20 đơn vị — *Overselling*).
2. **Giải pháp nguyên tử trong `OrderService`:**
   - Mọi thao tác đọc giá, thêm `Order`, thêm `OrderDetail` và trừ tồn kho đều nằm trong **cùng một `IDbContextTransaction`**.
   - Bước trừ tồn kho sử dụng câu lệnh tham số hóa nguyên tử tại SQL Server:
     ```sql
     UPDATE [i]
     SET [i].[StockQuantity] = [i].[StockQuantity] - @__quantity
     FROM [dbo].[Item] AS [i]
     WHERE [i].[ItemID] = @__itemId AND [i].[StockQuantity] >= @__quantity;
     ```
   - Khi `T1` chạy lệnh `UPDATE` này, SQL Server cấp khóa độc quyền dòng (`X` row lock) trên `ItemID = 15` cho đến khi `T1` kết thúc giao dịch.
   - `T2` chạy đến cùng câu lệnh `UPDATE` sẽ bị SQL Server giữ chờ khóa dòng. Ngay khi `T1` `Commit` (`StockQuantity` còn `10`), SQL Server đánh thức `T2` và đánh giá lại điều kiện `WHERE StockQuantity >= 30` trên giá trị mới `10` -> điều kiện sai -> `affectedRows = 0 != 1`.
   - `OrderService` phát hiện `affectedRows != 1`, lập tức gọi `await transaction.RollbackAsync()`, hoàn tác toàn bộ `Order`, `OrderDetail` và mọi mặt hàng đã trừ trước đó của `T2`.
   - Ngoài ra, các dòng đặt hàng được chuẩn hóa và **sắp xếp tăng dần theo `ItemID`** trước khi khóa dòng, triệt tiêu hoàn toàn nguy cơ deadlock vòng tròn khi hai đại lý đặt trùng tập sản phẩm theo thứ tự ngược nhau.
