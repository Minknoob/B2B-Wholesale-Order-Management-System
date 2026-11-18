namespace B2B.Core;

/// <summary>Shared account-lockout rules for WinForms and the Agent Portal.</summary>
public static class AccountLockout
{
    public const int MaxFailedAttempts = 3;

    public static int RecordFailedAttempt(int currentCount) =>
        Math.Min(Math.Max(currentCount, 0) + 1, MaxFailedAttempts);

    public static bool ShouldLock(int failedLoginCount) =>
        failedLoginCount >= MaxFailedAttempts;

    public static bool CanAttemptLogin(bool isLocked) => !isLocked;

    public static int ResetAfterSuccessfulLogin() => 0;
}
