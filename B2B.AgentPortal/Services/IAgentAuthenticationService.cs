namespace B2B.AgentPortal.Services;

public enum AgentLoginFailureReason
{
    None = 0,
    InvalidCredentials = 1,
    AdminRoleDenied = 2,
    InvalidRole = 3,
    AccountLocked = 4,
    MissingAgentMapping = 5,
    SystemFailure = 6
}

public sealed record AgentAuthenticationResult
{
    public bool Succeeded { get; init; }

    public int UserId { get; init; }

    public string UserName { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public int? AgentId { get; init; }

    public string? AgentName { get; init; }

    public AgentLoginFailureReason FailureReason { get; init; }

    public string ErrorMessage { get; init; } = string.Empty;

    public static AgentAuthenticationResult Success(
        int userId,
        string userName,
        string role,
        int agentId,
        string agentName) =>
        new()
        {
            Succeeded = true,
            UserId = userId,
            UserName = userName,
            Role = role,
            AgentId = agentId,
            AgentName = agentName,
            FailureReason = AgentLoginFailureReason.None,
            ErrorMessage = string.Empty
        };

    public static AgentAuthenticationResult Failure(
        AgentLoginFailureReason reason,
        string errorMessage) =>
        new()
        {
            Succeeded = false,
            FailureReason = reason,
            ErrorMessage = errorMessage
        };
}

public interface IAgentAuthenticationService
{
    Task<AgentAuthenticationResult> AuthenticateAgentAsync(
        string userName,
        string password,
        CancellationToken cancellationToken = default);
}
