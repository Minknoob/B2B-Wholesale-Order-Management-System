using System.Security.Claims;
using B2B.AgentPortal.Data;
using B2B.AgentPortal.Security;
using B2B.AgentPortal.Services;
using B2B.AgentPortal.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace B2B.AgentPortal.Controllers;

[Authorize(Roles = AgentPortalClaims.AgentRole)]
public partial class OrdersController : Controller
{
    private readonly IOrderService _orderService;
    private readonly B2BDbContext _dbContext;

    public OrdersController(IOrderService orderService, B2BDbContext dbContext)
    {
        _orderService = orderService;
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Create(
        int? itemId = null,
        int? quantity = null,
        CancellationToken cancellationToken = default)
    {
        if (!User.TryGetAgentId(out _))
        {
            return await ForceReLoginAsync();
        }

        IReadOnlyList<CatalogItemViewModel> catalogItems = await LoadCatalogItemsAsync(cancellationToken);

        var initialLines = new List<OrderLineInputViewModel>();
        if (itemId.HasValue && itemId.Value > 0)
        {
            CatalogItemViewModel? preselected = catalogItems.FirstOrDefault(i => i.ItemID == itemId.Value && i.IsInStock);
            if (preselected is not null)
            {
                int safeQty = Math.Clamp(quantity ?? 1, 1, preselected.StockQuantity);
                initialLines.Add(new OrderLineInputViewModel
                {
                    ItemID = preselected.ItemID,
                    Quantity = safeQty
                });
            }
        }

        if (initialLines.Count == 0)
        {
            CatalogItemViewModel? firstAvailable = catalogItems.FirstOrDefault(i => i.IsInStock);
            initialLines.Add(new OrderLineInputViewModel
            {
                ItemID = firstAvailable?.ItemID ?? 0,
                Quantity = 1
            });
        }

        var viewModel = new CreateOrderViewModel
        {
            Lines = initialLines,
            AvailableItems = catalogItems,
            AgentDisplayName = GetAgentDisplayName()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateOrderViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!User.TryGetAgentId(out int agentId))
        {
            return await ForceReLoginAsync();
        }

        model ??= new CreateOrderViewModel();
        model.Lines ??= new List<OrderLineInputViewModel>();

        if (model.Lines.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Đơn hàng phải có ít nhất một mặt hàng.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateReferenceDataAsync(model, cancellationToken);
            return View(model);
        }

        List<OrderLineRequest> lineRequests = model.Lines
            .Select(l => new OrderLineRequest(l.ItemID, l.Quantity))
            .ToList();

        PlaceOrderResult result = await _orderService.PlaceOrderAsync(
            agentId,
            lineRequests,
            cancellationToken);

        if (!result.Success || !result.OrderId.HasValue)
        {
            ModelState.AddModelError(
                string.Empty,
                result.ErrorMessage ?? "Không thể hoàn tất đơn hàng. Vui lòng kiểm tra lại dữ liệu.");

            await PopulateReferenceDataAsync(model, cancellationToken);
            return View(model);
        }

        TempData["OrderSuccessMessage"] = $"Đặt hàng thành công. Mã đơn #{result.OrderId.Value} đã được ghi nhận ở trạng thái Pending.";
        return RedirectToAction(nameof(Confirmation), new { id = result.OrderId.Value });
    }

    [HttpGet]
    public async Task<IActionResult> Confirmation(int id, CancellationToken cancellationToken = default)
    {
        if (!User.TryGetAgentId(out int agentId))
        {
            return await ForceReLoginAsync();
        }

        OrderLookupResult lookup = await _orderService.GetOrderForAgentAsync(id, agentId, cancellationToken);

        if (lookup.AccessStatus == OrderAccessStatus.NotFound)
        {
            return NotFound();
        }

        if (lookup.AccessStatus == OrderAccessStatus.Forbidden || lookup.Order is null)
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return View("~/Views/Account/AccessDenied.cshtml");
        }

        OrderSummaryDto summary = lookup.Order;
        var viewModel = new OrderConfirmationViewModel
        {
            OrderID = summary.OrderId,
            OrderDate = summary.OrderDate,
            OrderStatus = summary.OrderStatus,
            AgentID = summary.AgentId,
            AgentName = summary.AgentName,
            AgentAddress = summary.AgentAddress,
            AgentPhone = summary.AgentPhone,
            TotalAmount = summary.TotalAmount,
            Lines = summary.Lines
                .Select(l => new OrderLineResultViewModel
                {
                    ItemID = l.ItemId,
                    ItemName = l.ItemName,
                    Size = l.Size,
                    Quantity = l.Quantity,
                    UnitAmount = l.UnitAmount,
                    LineTotal = l.LineTotal
                })
                .ToList()
        };

        return View(viewModel);
    }

    private async Task PopulateReferenceDataAsync(CreateOrderViewModel model, CancellationToken cancellationToken)
    {
        model.AvailableItems = await LoadCatalogItemsAsync(cancellationToken);
        model.AgentDisplayName = GetAgentDisplayName();

        if (model.Lines.Count == 0)
        {
            CatalogItemViewModel? firstAvailable = model.AvailableItems.FirstOrDefault(i => i.IsInStock);
            model.Lines.Add(new OrderLineInputViewModel
            {
                ItemID = firstAvailable?.ItemID ?? 0,
                Quantity = 1
            });
        }
    }

    private async Task<IReadOnlyList<CatalogItemViewModel>> LoadCatalogItemsAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Items
            .AsNoTracking()
            .OrderBy(i => i.ItemName)
            .ThenBy(i => i.ItemID)
            .Select(i => new CatalogItemViewModel
            {
                ItemID = i.ItemID,
                ItemName = i.ItemName,
                Size = i.Size,
                Price = i.Price,
                StockQuantity = i.StockQuantity
            })
            .ToListAsync(cancellationToken);
    }

    private string GetAgentDisplayName()
    {
        string? agentName = User.FindFirstValue(AgentPortalClaims.AgentNameClaimType);
        return !string.IsNullOrWhiteSpace(agentName)
            ? agentName
            : (User.Identity?.Name ?? "Agent");
    }

    private async Task<IActionResult> ForceReLoginAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["AuthError"] = "Phiên đăng nhập thiếu thông tin Đại lý (AgentID) hợp lệ. Vui lòng đăng nhập lại.";
        return RedirectToAction("Login", "Account");
    }
}
