using System.Data;
using B2B.AgentPortal.Data;
using B2B.AgentPortal.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace B2B.AgentPortal.Services;

public class OrderService : IOrderService
{
    private readonly B2BDbContext _dbContext;
    private readonly ILogger<OrderService> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly Func<CancellationToken, Task>? _beforeCommitHook;

    public OrderService(
        B2BDbContext dbContext,
        ILogger<OrderService> logger,
        TimeProvider? timeProvider = null)
        : this(dbContext, logger, timeProvider, beforeCommitHook: null)
    {
    }

    /// <summary>
    /// Additional constructor enabling deterministic fault-injection right before transaction commit during testing.
    /// </summary>
    public OrderService(
        B2BDbContext dbContext,
        ILogger<OrderService> logger,
        TimeProvider? timeProvider,
        Func<CancellationToken, Task>? beforeCommitHook)
    {
        _dbContext = dbContext;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _beforeCommitHook = beforeCommitHook;
    }

    public async Task<PlaceOrderResult> PlaceOrderAsync(
        int agentId,
        IReadOnlyCollection<OrderLineRequest> lines,
        CancellationToken cancellationToken = default)
    {
        if (agentId <= 0)
        {
            return PlaceOrderResult.Failure(
                OrderErrorCode.InvalidAgent,
                "Thông tin Đại lý (AgentID) không hợp lệ. Vui lòng đăng nhập lại.");
        }

        if (lines is null || lines.Count == 0)
        {
            return PlaceOrderResult.Failure(
                OrderErrorCode.EmptyOrder,
                "Đơn hàng phải có ít nhất một mặt hàng.");
        }

        var mergedQuantities = new Dictionary<int, int>();
        foreach (OrderLineRequest? line in lines)
        {
            if (line is null || line.ItemId <= 0)
            {
                return PlaceOrderResult.Failure(
                    OrderErrorCode.InvalidItem,
                    "Danh sách mặt hàng có mã sản phẩm không hợp lệ.");
            }

            if (line.Quantity <= 0)
            {
                return PlaceOrderResult.Failure(
                    OrderErrorCode.InvalidQuantity,
                    "Số lượng đặt của mỗi dòng phải là số nguyên lớn hơn 0.");
            }

            if (mergedQuantities.TryGetValue(line.ItemId, out int currentTotal))
            {
                try
                {
                    mergedQuantities[line.ItemId] = checked(currentTotal + line.Quantity);
                }
                catch (OverflowException)
                {
                    return PlaceOrderResult.Failure(
                        OrderErrorCode.InvalidQuantity,
                        "Tổng số lượng đặt sau khi gộp các dòng trùng vượt quá giới hạn cho phép.");
                }
            }
            else
            {
                mergedQuantities[line.ItemId] = line.Quantity;
            }
        }

        // Deterministic ascending ItemID order eliminates AB-BA row-lock deadlocks when two concurrent orders share items.
        List<KeyValuePair<int, int>> normalizedLines = mergedQuantities
            .OrderBy(kvp => kvp.Key)
            .ToList();

        if (_dbContext.Database.IsRelational())
        {
            return await PlaceOrderRelationalTransactionAsync(agentId, normalizedLines, cancellationToken);
        }

        return await PlaceOrderInMemoryAsync(agentId, normalizedLines, cancellationToken);
    }

    public async Task<OrderLookupResult> GetOrderForAgentAsync(
        int orderId,
        int agentId,
        CancellationToken cancellationToken = default)
    {
        if (orderId <= 0)
        {
            return new OrderLookupResult(OrderAccessStatus.NotFound);
        }

        Order? order = await _dbContext.Orders
            .AsNoTracking()
            .Include(o => o.Agent)
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Item)
            .FirstOrDefaultAsync(o => o.OrderID == orderId, cancellationToken);

        if (order is null)
        {
            return new OrderLookupResult(OrderAccessStatus.NotFound);
        }

        if (!order.BelongsToAgent(agentId))
        {
            return new OrderLookupResult(OrderAccessStatus.Forbidden);
        }

        List<PlacedOrderLineDto> lineDtos = order.OrderDetails
            .OrderBy(d => d.ID)
            .Select(d => new PlacedOrderLineDto(
                d.ItemID,
                d.Item?.ItemName ?? $"Item #{d.ItemID}",
                d.Item?.Size ?? string.Empty,
                d.Quantity,
                d.UnitAmount,
                d.CalculateLineTotal()))
            .ToList();

        decimal totalAmount = order.CalculateTotalAmount();

        return new OrderLookupResult(
            OrderAccessStatus.Found,
            new OrderSummaryDto(
                order.OrderID,
                order.OrderDate,
                order.OrderStatus,
                order.AgentID,
                order.Agent?.AgentName ?? string.Empty,
                order.Agent?.Address ?? string.Empty,
                order.Agent?.Phone ?? string.Empty,
                lineDtos,
                totalAmount));
    }

    private async Task<PlaceOrderResult> PlaceOrderRelationalTransactionAsync(
        int agentId,
        IReadOnlyList<KeyValuePair<int, int>> normalizedLines,
        CancellationToken cancellationToken)
    {
        await using IDbContextTransaction transaction =
            await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        try
        {
            bool agentExists = await _dbContext.Agents
                .AsNoTracking()
                .AnyAsync(a => a.AgentID == agentId, cancellationToken);

            if (!agentExists)
            {
                await transaction.RollbackAsync(cancellationToken);
                return PlaceOrderResult.Failure(
                    OrderErrorCode.InvalidAgent,
                    "Không tìm thấy hồ sơ Đại lý hợp lệ trong hệ thống.");
            }

            int[] requestedItemIds = normalizedLines.Select(l => l.Key).ToArray();

            Dictionary<int, Item> itemsById = await _dbContext.Items
                .AsNoTracking()
                .Where(i => requestedItemIds.Contains(i.ItemID))
                .ToDictionaryAsync(i => i.ItemID, cancellationToken);

            if (itemsById.Count != requestedItemIds.Length)
            {
                await transaction.RollbackAsync(cancellationToken);
                return PlaceOrderResult.Failure(
                    OrderErrorCode.InvalidItem,
                    "Một hoặc nhiều mặt hàng được chọn không tồn tại trong hệ thống.");
            }

            foreach ((int itemId, int quantity) in normalizedLines)
            {
                Item dbItem = itemsById[itemId];
                if (dbItem.StockQuantity <= 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return PlaceOrderResult.Failure(
                        OrderErrorCode.InsufficientStock,
                        $"Mặt hàng '{dbItem.ItemName}' hiện đã hết hàng.");
                }

                if (dbItem.StockQuantity < quantity)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return PlaceOrderResult.Failure(
                        OrderErrorCode.InsufficientStock,
                        $"Mặt hàng '{dbItem.ItemName}' chỉ còn {dbItem.StockQuantity} đơn vị tồn kho, không đủ cho số lượng đặt ({quantity}).");
                }
            }

            DateTime orderDate = GetCurrentOrderTimestamp();
            var order = new Order
            {
                AgentID = agentId,
                OrderDate = orderDate,
                OrderStatus = OrderStatuses.Pending
            };

            var finalizedLines = new List<PlacedOrderLineDto>(normalizedLines.Count);

            foreach ((int itemId, int quantity) in normalizedLines)
            {
                Item dbItem = itemsById[itemId];
                decimal unitAmount = dbItem.Price;
                decimal lineTotal = checked(quantity * unitAmount);

                order.OrderDetails.Add(new OrderDetail
                {
                    ItemID = itemId,
                    Quantity = quantity,
                    UnitAmount = unitAmount
                });

                finalizedLines.Add(new PlacedOrderLineDto(
                    itemId,
                    dbItem.ItemName,
                    dbItem.Size,
                    quantity,
                    unitAmount,
                    lineTotal));
            }

            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Conditional atomic stock deduction inside the same database transaction prevents overselling and lost updates.
            foreach ((int itemId, int quantity) in normalizedLines)
            {
                int affectedRows = await _dbContext.Items
                    .Where(i => i.ItemID == itemId && i.StockQuantity >= quantity)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(i => i.StockQuantity, i => i.StockQuantity - quantity),
                        cancellationToken);

                if (affectedRows != 1)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _dbContext.ChangeTracker.Clear();

                    Item dbItem = itemsById[itemId];
                    return PlaceOrderResult.Failure(
                        OrderErrorCode.InsufficientStock,
                        $"Tồn kho của mặt hàng '{dbItem.ItemName}' vừa thay đổi và không còn đủ số lượng ({quantity}). Toàn bộ đơn hàng đã được hoàn tác.");
                }
            }

            if (_beforeCommitHook is not null)
            {
                await _beforeCommitHook(cancellationToken);
            }

            decimal totalAmount = finalizedLines.Sum(l => l.LineTotal);
            await transaction.CommitAsync(cancellationToken);

            return PlaceOrderResult.Succeeded(
                order.OrderID,
                order.OrderDate,
                order.OrderStatus,
                finalizedLines,
                totalAmount);
        }
        catch (OperationCanceledException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            _dbContext.ChangeTracker.Clear();
            throw;
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            _dbContext.ChangeTracker.Clear();
            _logger.LogWarning(ex, "Concurrency or constraint conflict while placing order for AgentID {AgentId}.", agentId);

            return PlaceOrderResult.Failure(
                OrderErrorCode.ConcurrencyConflict,
                "Đơn hàng không thể hoàn tất do xung đột cập nhật dữ liệu đồng thời. Vui lòng kiểm tra lại tồn kho và thử lại.");
        }
        catch (SqlException ex) when (ex.Number is 1205 or 1222)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            _dbContext.ChangeTracker.Clear();
            _logger.LogWarning(ex, "SQL lock/deadlock conflict while placing order for AgentID {AgentId}.", agentId);

            return PlaceOrderResult.Failure(
                OrderErrorCode.ConcurrencyConflict,
                "Hệ thống đang xử lý giao dịch cạnh tranh tồn kho. Vui lòng gửi lại đơn hàng.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            _dbContext.ChangeTracker.Clear();
            _logger.LogError(ex, "Order transaction failed and rolled back completely for AgentID {AgentId}.", agentId);

            return PlaceOrderResult.Failure(
                OrderErrorCode.UnexpectedFailure,
                "Đã xảy ra lỗi trong quá trình xử lý giao dịch đặt hàng. Không có dữ liệu nào bị thay đổi, vui lòng thử lại.");
        }
    }

    private async Task<PlaceOrderResult> PlaceOrderInMemoryAsync(
        int agentId,
        IReadOnlyList<KeyValuePair<int, int>> normalizedLines,
        CancellationToken cancellationToken)
    {
        try
        {
            bool agentExists = await _dbContext.Agents
                .AsNoTracking()
                .AnyAsync(a => a.AgentID == agentId, cancellationToken);

            if (!agentExists)
            {
                return PlaceOrderResult.Failure(
                    OrderErrorCode.InvalidAgent,
                    "Không tìm thấy hồ sơ Đại lý hợp lệ trong hệ thống.");
            }

            int[] requestedItemIds = normalizedLines.Select(l => l.Key).ToArray();

            Dictionary<int, Item> trackedItemsById = await _dbContext.Items
                .Where(i => requestedItemIds.Contains(i.ItemID))
                .ToDictionaryAsync(i => i.ItemID, cancellationToken);

            if (trackedItemsById.Count != requestedItemIds.Length)
            {
                return PlaceOrderResult.Failure(
                    OrderErrorCode.InvalidItem,
                    "Một hoặc nhiều mặt hàng được chọn không tồn tại trong hệ thống.");
            }

            foreach ((int itemId, int quantity) in normalizedLines)
            {
                Item item = trackedItemsById[itemId];
                if (item.StockQuantity <= 0)
                {
                    return PlaceOrderResult.Failure(
                        OrderErrorCode.InsufficientStock,
                        $"Mặt hàng '{item.ItemName}' hiện đã hết hàng.");
                }

                if (item.StockQuantity < quantity)
                {
                    return PlaceOrderResult.Failure(
                        OrderErrorCode.InsufficientStock,
                        $"Mặt hàng '{item.ItemName}' chỉ còn {item.StockQuantity} đơn vị tồn kho, không đủ cho số lượng đặt ({quantity}).");
                }
            }

            if (_beforeCommitHook is not null)
            {
                await _beforeCommitHook(cancellationToken);
            }

            DateTime orderDate = GetCurrentOrderTimestamp();
            var order = new Order
            {
                AgentID = agentId,
                OrderDate = orderDate,
                OrderStatus = OrderStatuses.Pending
            };

            var finalizedLines = new List<PlacedOrderLineDto>(normalizedLines.Count);

            foreach ((int itemId, int quantity) in normalizedLines)
            {
                Item item = trackedItemsById[itemId];
                decimal unitAmount = item.Price;
                decimal lineTotal = checked(quantity * unitAmount);

                item.StockQuantity -= quantity;

                order.OrderDetails.Add(new OrderDetail
                {
                    ItemID = itemId,
                    Quantity = quantity,
                    UnitAmount = unitAmount
                });

                finalizedLines.Add(new PlacedOrderLineDto(
                    itemId,
                    item.ItemName,
                    item.Size,
                    quantity,
                    unitAmount,
                    lineTotal));
            }

            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync(cancellationToken);

            decimal totalAmount = finalizedLines.Sum(l => l.LineTotal);
            return PlaceOrderResult.Succeeded(
                order.OrderID,
                order.OrderDate,
                order.OrderStatus,
                finalizedLines,
                totalAmount);
        }
        catch (OperationCanceledException)
        {
            _dbContext.ChangeTracker.Clear();
            throw;
        }
        catch (Exception ex)
        {
            _dbContext.ChangeTracker.Clear();
            _logger.LogError(ex, "In-memory order processing failed for AgentID {AgentId}.", agentId);

            return PlaceOrderResult.Failure(
                OrderErrorCode.UnexpectedFailure,
                "Đã xảy ra lỗi trong quá trình xử lý đặt hàng. Không có dữ liệu nào bị thay đổi.");
        }
    }

    private DateTime GetCurrentOrderTimestamp()
    {
        DateTime localNow = _timeProvider.GetLocalNow().DateTime;
        return new DateTime(
            localNow.Year,
            localNow.Month,
            localNow.Day,
            localNow.Hour,
            localNow.Minute,
            localNow.Second,
            DateTimeKind.Unspecified);
    }
}
