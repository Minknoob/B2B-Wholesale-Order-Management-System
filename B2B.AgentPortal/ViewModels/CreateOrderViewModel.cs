using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace B2B.AgentPortal.ViewModels;

public class CreateOrderViewModel
{
    public List<OrderLineInputViewModel> Lines { get; set; } = new();

    [BindNever]
    [ValidateNever]
    public IReadOnlyList<CatalogItemViewModel> AvailableItems { get; set; } = Array.Empty<CatalogItemViewModel>();

    [BindNever]
    [ValidateNever]
    public string AgentDisplayName { get; set; } = string.Empty;
}
