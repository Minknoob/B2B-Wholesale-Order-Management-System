namespace B2B.AgentPortal.Models;

/// <summary>
/// Valid order status constants matching CK_Order_OrderStatus in M1 schema.
/// </summary>
public static class OrderStatuses
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Shipped = "Shipped";
    public const string Delivered = "Delivered";
    public const string Cancelled = "Cancelled";

    public static IReadOnlyList<string> All { get; } =
    [
        Pending,
        Confirmed,
        Shipped,
        Delivered,
        Cancelled
    ];

    public static bool IsValid(string? status) =>
        !string.IsNullOrWhiteSpace(status) &&
        All.Contains(status, StringComparer.Ordinal);
}
