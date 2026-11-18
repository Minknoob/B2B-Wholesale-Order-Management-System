using System.Globalization;
using System.Security.Claims;
using B2B.AgentPortal.Security;
using B2B.AgentPortal.Services;
using B2B.AgentPortal.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2B.AgentPortal.Controllers;

[Authorize]
public class AccountController : Controller
{
    private readonly IAgentAuthenticationService _authService;

    public AccountController(IAgentAuthenticationService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.TryGetAgentId(out _))
        {
            return RedirectToSafeLocalUrl(returnUrl);
        }

        string? safeReturnUrl = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : null;

        return View(new LoginViewModel { ReturnUrl = safeReturnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        string? safeReturnUrl = !string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)
            ? model.ReturnUrl
            : null;
        model.ReturnUrl = safeReturnUrl;

        if (!ModelState.IsValid)
        {
            model.Password = string.Empty;
            return View(model);
        }

        AgentAuthenticationResult result = await _authService.AuthenticateAgentAsync(
            model.UserName,
            model.Password,
            cancellationToken);

        if (!result.Succeeded || !result.AgentId.HasValue)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            model.Password = string.Empty;
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.UserId.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, result.UserName),
            new(ClaimTypes.Role, AgentPortalClaims.AgentRole),
            new(AgentPortalClaims.AgentIdClaimType, result.AgentId.Value.ToString(CultureInfo.InvariantCulture))
        };

        if (!string.IsNullOrWhiteSpace(result.AgentName))
        {
            claims.Add(new Claim(AgentPortalClaims.AgentNameClaimType, result.AgentName));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                AllowRefresh = true,
                IsPersistent = false
            });

        return RedirectToSafeLocalUrl(safeReturnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["AuthNotice"] = "Bạn đã đăng xuất khỏi hệ thống an toàn.";
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [Route("/Error")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View("~/Views/Shared/Error.cshtml");
    }

    private IActionResult RedirectToSafeLocalUrl(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Catalog");
    }
}
