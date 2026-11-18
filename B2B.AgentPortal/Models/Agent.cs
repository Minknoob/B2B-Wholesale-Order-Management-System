namespace B2B.AgentPortal.Models;

public class Agent
{
    public int AgentID { get; set; }

    public string AgentName { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public ICollection<User> Users { get; set; } = new List<User>();

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
