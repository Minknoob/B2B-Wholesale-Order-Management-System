namespace B2B.AgentPortal.Services;

public enum OrderErrorCode
{
    None = 0,
    EmptyOrder = 1,
    InvalidQuantity = 2,
    InvalidItem = 3,
    InsufficientStock = 4,
    InvalidAgent = 5,
    ConcurrencyConflict = 6,
    UnexpectedFailure = 7
}

public sealed record OrderLineRequest(int ItemId, int Quantity);

public sealed record PlacedOrderLineDto(
    int ItemId,
    string ItemName,
    string Size,
    int Quantity,
    decimal UnitAmount,
    decimal LineTotal);

public sealed record PlaceOrderResult
{
    public bool Success { get; init; }

    public int? OrderId { get; init; }

    public DateTime? OrderDate { get; init; }

    public string? Status { get; init; }

    public IReadOnlyList<PlacedOrderLineDto> Lines { get; init; } = Array.Empty<PlacedOrderLineDto>();

    public decimal TotalAmount { get; init; }

    public OrderErrorCode ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }

    public static PlaceOrderResult Succeeded(
        int orderId,
        DateTime orderDate,
        string status,
        IReadOnlyList<PlacedOrderLineDto> lines,
        decimal totalAmount) =>
        new()
        {
            Success = true,
            OrderId = orderId,
            OrderDate = orderDate,
            Status = status,
            Lines = lines,
            TotalAmount = totalAmount,
            ErrorCode = OrderErrorCode.None,
            ErrorMessage = null
        };

    public static PlaceOrderResult Failure(OrderErrorCode errorCode, string errorMessage) =>
        new()
        {
            Success = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
}

public enum OrderAccessStatus
{
    Found = 0,
    NotFound = 1,
    Forbidden = 2
}

public sealed record OrderSummaryDto(
    int OrderId,
    DateTime OrderDate,
    string OrderStatus,
    int AgentId,
    string AgentName,
    string AgentAddress,
    string AgentPhone,
    IReadOnlyList<PlacedOrderLineDto> Lines,
    decimal TotalAmount);

public sealed record OrderLookupResult(
    OrderAccessStatus AccessStatus,
    OrderSummaryDto? Order = null);

public interface IOrderService
{
    Task<PlaceOrderResult> PlaceOrderAsync(
        int agentId,
        IReadOnlyCollection<OrderLineRequest> lines,
        CancellationToken cancellationToken = default);

    Task<OrderLookupResult> GetOrderForAgentAsync(
        int orderId,
        int agentId,
        CancellationToken cancellationToken = default);
}
