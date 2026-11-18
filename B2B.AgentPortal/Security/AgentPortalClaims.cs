using System.Globalization;
using System.Security.Claims;

namespace B2B.AgentPortal.Security;

/// <summary>
/// Shared authentication claim types and role constants for the Agent Portal (reusable by M4 and M5).
/// </summary>
public static class AgentPortalClaims
{
    public const string AgentIdClaimType = "AgentID";
    public const string AgentNameClaimType = "AgentName";
    public const string AgentRole = "Agent";
    public const string AdminRole = "Admin";

    public static bool TryGetAgentId(this ClaimsPrincipal? principal, out int agentId)
    {
        agentId = 0;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (!principal.IsInRole(AgentRole))
        {
            return false;
        }

        string? rawValue = principal.FindFirstValue(AgentIdClaimType);
        return int.TryParse(rawValue, NumberStyles.None, CultureInfo.InvariantCulture, out agentId)
            && agentId > 0;
    }

    public static bool TryGetUserId(this ClaimsPrincipal? principal, out int userId)
    {
        userId = 0;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        string? rawValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(rawValue, NumberStyles.None, CultureInfo.InvariantCulture, out userId)
            && userId > 0;
    }
}
