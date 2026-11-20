using System.Data;
using B2B.AgentPortal.Data;
using B2B.AgentPortal.Models;
using B2B.AgentPortal.Security;
using B2B.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace B2B.AgentPortal.Services;

public class AgentAuthenticationService : IAgentAuthenticationService
{
    private const string GenericInvalidCredentialsMessage = "Tên đăng nhập hoặc mật khẩu không đúng.";

    private readonly B2BDbContext _dbContext;
    private readonly ILogger<AgentAuthenticationService> _logger;

    public AgentAuthenticationService(
        B2BDbContext dbContext,
        ILogger<AgentAuthenticationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<AgentAuthenticationResult> AuthenticateAgentAsync(
        string userName,
        string password,
        CancellationToken cancellationToken = default)
    {
        string normalizedUserName = (userName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(normalizedUserName) || string.IsNullOrEmpty(password))
        {
            return AgentAuthenticationResult.Failure(
                AgentLoginFailureReason.InvalidCredentials,
                GenericInvalidCredentialsMessage);
        }

        bool isRelational = _dbContext.Database.IsRelational();
        bool isSqlServer = _dbContext.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true;
        IDbContextTransaction? transaction = null;

        try
        {
            if (isRelational)
            {
                IsolationLevel isolationLevel = isSqlServer
                    ? IsolationLevel.Serializable
                    : IsolationLevel.ReadCommitted;
                transaction = await _dbContext.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
            }

            User? user;
            if (isSqlServer)
            {
                user = await _dbContext.Users
                    .FromSqlInterpolated($"SELECT * FROM dbo.Users WITH (UPDLOCK, ROWLOCK) WHERE UserName = {normalizedUserName}")
                    .Include(u => u.Agent)
                    .FirstOrDefaultAsync(cancellationToken);
            }
            else
            {
                user = await _dbContext.Users
                    .Include(u => u.Agent)
                    .FirstOrDefaultAsync(u => u.UserName == normalizedUserName, cancellationToken);
            }

            if (user is null)
            {
                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                return AgentAuthenticationResult.Failure(
                    AgentLoginFailureReason.InvalidCredentials,
                    GenericInvalidCredentialsMessage);
            }

            if (string.Equals(user.Role, AgentPortalClaims.AdminRole, StringComparison.OrdinalIgnoreCase))
            {
                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                return AgentAuthenticationResult.Failure(
                    AgentLoginFailureReason.AdminRoleDenied,
                    "Tài khoản này thuộc quyền Quản trị viên (Admin). Chỉ tài khoản Đại lý (Agent) mới được phép đăng nhập Agent Portal.");
            }

            if (!string.Equals(user.Role, AgentPortalClaims.AgentRole, StringComparison.OrdinalIgnoreCase))
            {
                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                return AgentAuthenticationResult.Failure(
                    AgentLoginFailureReason.InvalidRole,
                    "Tài khoản không có quyền truy cập Agent Portal.");
            }

            if (!AccountLockout.CanAttemptLogin(user.IsLocked))
            {
                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                return AgentAuthenticationResult.Failure(
                    AgentLoginFailureReason.AccountLocked,
                    "Tài khoản đã bị khóa do nhập sai mật khẩu 3 lần. Vui lòng liên hệ Quản trị viên để mở khóa.");
            }

            if (!PasswordHasher.VerifyPassword(password, user.PasswordHash))
            {
                int nextFailedCount = AccountLockout.RecordFailedAttempt(user.FailedLoginCount);
                bool shouldLock = AccountLockout.ShouldLock(nextFailedCount);

                user.FailedLoginCount = nextFailedCount;
                user.IsLocked = shouldLock;

                await _dbContext.SaveChangesAsync(cancellationToken);

                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                if (shouldLock)
                {
                    _logger.LogWarning("Agent user ID {UserId} has been locked after reaching the maximum failed login attempts.", user.UserID);
                    return AgentAuthenticationResult.Failure(
                        AgentLoginFailureReason.AccountLocked,
                        "Bạn đã nhập sai mật khẩu 3 lần liên tiếp. Tài khoản đã bị khóa, vui lòng liên hệ Quản trị viên để mở khóa.");
                }

                return AgentAuthenticationResult.Failure(
                    AgentLoginFailureReason.InvalidCredentials,
                    GenericInvalidCredentialsMessage);
            }

            if (!user.AgentID.HasValue || user.AgentID.Value <= 0 || user.Agent is null)
            {
                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                _logger.LogWarning("Agent user ID {UserId} does not have a valid AgentID mapping.", user.UserID);
                return AgentAuthenticationResult.Failure(
                    AgentLoginFailureReason.MissingAgentMapping,
                    "Tài khoản Đại lý chưa được liên kết với hồ sơ Agent hợp lệ. Vui lòng liên hệ Quản trị viên.");
            }

            int resetCount = AccountLockout.ResetAfterSuccessfulLogin();
            if (user.FailedLoginCount != resetCount)
            {
                user.FailedLoginCount = resetCount;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return AgentAuthenticationResult.Success(
                user.UserID,
                user.UserName,
                AgentPortalClaims.AgentRole,
                user.AgentID.Value,
                user.Agent.AgentName);
        }
        catch (OperationCanceledException)
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }

            _dbContext.ChangeTracker.Clear();
            throw;
        }
        catch (Exception ex)
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }

            _dbContext.ChangeTracker.Clear();
            _logger.LogError(ex, "Unexpected database error while processing Agent login.");

            return AgentAuthenticationResult.Failure(
                AgentLoginFailureReason.SystemFailure,
                "Hệ thống tạm thời không thể xử lý đăng nhập. Vui lòng thử lại sau.");
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }
}
