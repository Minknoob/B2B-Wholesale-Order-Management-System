namespace B2B.AgentPortal.Models;

public class User
{
    public int UserID { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool IsLocked { get; set; }

    public string Role { get; set; } = string.Empty;

    public int? AgentID { get; set; }

    public int FailedLoginCount { get; set; }

    public Agent? Agent { get; set; }
}
