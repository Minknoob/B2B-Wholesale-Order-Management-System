namespace B2B.AgentPortal.ViewModels;

public class OrderConfirmationViewModel
{
    public int OrderID { get; init; }

    public DateTime OrderDate { get; init; }

    public string OrderStatus { get; init; } = string.Empty;

    public int AgentID { get; init; }

    public string AgentName { get; init; } = string.Empty;

    public string AgentAddress { get; init; } = string.Empty;

    public string AgentPhone { get; init; } = string.Empty;

    public IReadOnlyList<OrderLineResultViewModel> Lines { get; init; } = Array.Empty<OrderLineResultViewModel>();

    public decimal TotalAmount { get; init; }
}
