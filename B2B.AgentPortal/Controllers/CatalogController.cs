using B2B.AgentPortal.Data;
using B2B.AgentPortal.Security;
using B2B.AgentPortal.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace B2B.AgentPortal.Controllers;

[Authorize(Roles = AgentPortalClaims.AgentRole)]
public class CatalogController : Controller
{
    private readonly B2BDbContext _dbContext;

    public CatalogController(B2BDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? q = null,
        bool inStockOnly = false,
        CancellationToken cancellationToken = default)
    {
        if (!User.TryGetAgentId(out _))
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["AuthError"] = "Phiên đăng nhập thiếu thông tin Đại lý hợp lệ. Vui lòng đăng nhập lại.";
            return RedirectToAction("Login", "Account");
        }

        List<CatalogItemViewModel> allItems = await _dbContext.Items
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

        int totalCount = allItems.Count;
        int inStockCount = allItems.Count(i => i.IsInStock);
        int outOfStockCount = totalCount - inStockCount;

        IEnumerable<CatalogItemViewModel> filtered = allItems;
        string? trimmedQuery = string.IsNullOrWhiteSpace(q) ? null : q.Trim();

        if (trimmedQuery is not null)
        {
            filtered = filtered.Where(i =>
                i.ItemName.Contains(trimmedQuery, StringComparison.OrdinalIgnoreCase) ||
                i.Size.Contains(trimmedQuery, StringComparison.OrdinalIgnoreCase));
        }

        if (inStockOnly)
        {
            filtered = filtered.Where(i => i.IsInStock);
        }

        var viewModel = new CatalogIndexViewModel
        {
            SearchQuery = trimmedQuery,
            InStockOnly = inStockOnly,
            TotalCatalogCount = totalCount,
            InStockCount = inStockCount,
            OutOfStockCount = outOfStockCount,
            Items = filtered.ToList()
        };

        return View(viewModel);
    }
}
