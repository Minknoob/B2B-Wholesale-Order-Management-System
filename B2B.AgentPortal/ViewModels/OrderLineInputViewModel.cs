using System.ComponentModel.DataAnnotations;

namespace B2B.AgentPortal.ViewModels;

/// <summary>
/// Untrusted client input for a single order line. Contains ONLY ItemID and Quantity.
/// UnitAmount, AgentID, and OrderStatus are never accepted from the client.
/// </summary>
public class OrderLineInputViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn mặt hàng.")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn mặt hàng hợp lệ.")]
    [Display(Name = "Mặt hàng")]
    public int ItemID { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số lượng.")]
    [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải là số nguyên lớn hơn 0.")]
    [Display(Name = "Số lượng")]
    public int Quantity { get; set; } = 1;
}
