namespace B2B.AgentPortal.ViewModels;

public class CatalogItemViewModel
{
    public int ItemID { get; init; }

    public string ItemName { get; init; } = string.Empty;

    public string Size { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public int StockQuantity { get; init; }

    public bool IsInStock => StockQuantity > 0;

    public bool IsLowStock => StockQuantity > 0 && StockQuantity < 10;
}

public class CatalogIndexViewModel
{
    public string? SearchQuery { get; init; }

    public bool InStockOnly { get; init; }

    public int TotalCatalogCount { get; init; }

    public int InStockCount { get; init; }

    public int OutOfStockCount { get; init; }

    public IReadOnlyList<CatalogItemViewModel> Items { get; init; } = Array.Empty<CatalogItemViewModel>();
}
