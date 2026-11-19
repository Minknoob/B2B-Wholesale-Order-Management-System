namespace B2B.AgentPortal.ViewModels;

public class OrderLineResultViewModel
{
    public int ItemID { get; init; }

    public string ItemName { get; init; } = string.Empty;

    public string Size { get; init; } = string.Empty;

    public int Quantity { get; init; }

    public decimal UnitAmount { get; init; }

    public decimal LineTotal { get; init; }
}
