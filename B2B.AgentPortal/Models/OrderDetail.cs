namespace B2B.AgentPortal.Models;

public class OrderDetail
{
    public int ID { get; set; }

    public int OrderID { get; set; }

    public int ItemID { get; set; }

    public int Quantity { get; set; }

    public decimal UnitAmount { get; set; }

    public Order Order { get; set; } = null!;

    public Item Item { get; set; } = null!;

    public decimal CalculateLineTotal() => Quantity * UnitAmount;
}
