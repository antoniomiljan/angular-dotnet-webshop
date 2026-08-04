namespace Api.DTOs;

public class DashboardDto
{
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int PendingOrdersCount { get; set; }
    public int ActiveProductsCount { get; set; }
    public List<OutOfStockProductDto> OutOfStockProducts { get; set; } = new();
    public List<TopProductDto> TopProducts { get; set; } = new();
}

public class OutOfStockProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class TopProductDto
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}
