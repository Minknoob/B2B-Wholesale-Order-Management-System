namespace B2B.AgentPortal.Models;

public class Order
{
    public int OrderID { get; set; }

    public DateTime OrderDate { get; set; }

    public int AgentID { get; set; }

    public string OrderStatus { get; set; } = OrderStatuses.Pending;

    public Agent Agent { get; set; } = null!;

    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    /// <summary>
    /// Computes total amount from OrderDetails using decimal arithmetic without adding an extra database column.
    /// </summary>
    public decimal CalculateTotalAmount() =>
        OrderDetails.Sum(detail => detail.Quantity * detail.UnitAmount);

    /// <summary>
    /// Checks whether this order belongs to the specified agent.
    /// </summary>
    public bool BelongsToAgent(int agentId) => agentId > 0 && AgentID == agentId;
}
