namespace B2B.AgentPortal.ViewModels;

public class OrderSummaryVm
{
    public int OrderId { get; set; }

    public DateTime OrderDate { get; set; }

    public string OrderStatus { get; set; } = string.Empty;

    public int LineCount { get; set; }

    public decimal Total { get; set; }
}

public class OrderLineVm
{
    public string ItemName { get; set; } = string.Empty;

    public string? Size { get; set; }

    public int Quantity { get; set; }

    public decimal UnitAmount { get; set; }

    public decimal LineTotal => Quantity * UnitAmount;
}

public class OrderDetailVm
{
    public int OrderId { get; set; }

    public DateTime OrderDate { get; set; }

    public string OrderStatus { get; set; } = string.Empty;

    public string AgentName { get; set; } = string.Empty;

    public string? AgentAddress { get; set; }

    public string? AgentPhone { get; set; }

    public List<OrderLineVm> Lines { get; set; } = new();

    public decimal Total => Lines.Sum(l => l.LineTotal);
}
