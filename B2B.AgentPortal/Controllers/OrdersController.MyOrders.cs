using B2B.AgentPortal.Security;
using B2B.AgentPortal.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace B2B.AgentPortal.Controllers;

public partial class OrdersController
{
    // GET /Orders -> My Orders (Task 3.3: chỉ hiển thị đơn có AgentID trùng với Agent đang đăng nhập)
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        if (!User.TryGetAgentId(out int agentId))
        {
            return await ForceReLoginAsync();
        }

        List<OrderSummaryVm> orders = await _dbContext.Orders
            .AsNoTracking()
            .Where(o => o.AgentID == agentId)
            .OrderByDescending(o => o.OrderDate)
            .ThenByDescending(o => o.OrderID)
            .Select(o => new OrderSummaryVm
            {
                OrderId = o.OrderID,
                OrderDate = o.OrderDate,
                OrderStatus = o.OrderStatus,
                LineCount = o.OrderDetails.Count,
                Total = o.OrderDetails.Sum(d => (decimal?)d.Quantity * d.UnitAmount) ?? 0m
            })
            .ToListAsync(cancellationToken);

        return View(orders);
    }

    // GET /Orders/Details/5
    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
    {
        if (!User.TryGetAgentId(out int agentId))
        {
            return await ForceReLoginAsync();
        }

        OrderDetailVm? vm = await LoadOwnedOrderAsync(id, agentId, cancellationToken);
        return vm is null ? NotFound() : View(vm);
    }

    // GET /Orders/Invoice/5 -> trang in hóa đơn
    [HttpGet]
    public async Task<IActionResult> Invoice(int id, CancellationToken cancellationToken = default)
    {
        if (!User.TryGetAgentId(out int agentId))
        {
            return await ForceReLoginAsync();
        }

        OrderDetailVm? vm = await LoadOwnedOrderAsync(id, agentId, cancellationToken);
        return vm is null ? NotFound() : View(vm);
    }

    // Kiểm tra quyền sở hữu ở server. Đơn của Agent khác trả về null
    // -> controller trả 404, không tiết lộ đơn đó có tồn tại.
    private async Task<OrderDetailVm?> LoadOwnedOrderAsync(
        int orderId,
        int agentId,
        CancellationToken cancellationToken)
    {
        if (orderId <= 0 || agentId <= 0)
        {
            return null;
        }

        return await _dbContext.Orders
            .AsNoTracking()
            .Where(o => o.OrderID == orderId && o.AgentID == agentId)
            .Select(o => new OrderDetailVm
            {
                OrderId = o.OrderID,
                OrderDate = o.OrderDate,
                OrderStatus = o.OrderStatus,
                AgentName = o.Agent.AgentName,
                AgentAddress = o.Agent.Address,
                AgentPhone = o.Agent.Phone,
                Lines = o.OrderDetails
                    .OrderBy(d => d.ID)
                    .Select(d => new OrderLineVm
                    {
                        ItemName = d.Item.ItemName,
                        Size = d.Item.Size,
                        Quantity = d.Quantity,
                        UnitAmount = d.UnitAmount
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
