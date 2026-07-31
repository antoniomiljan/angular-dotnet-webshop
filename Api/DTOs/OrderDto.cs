namespace Api.DTOs;

public class OrderDto
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? GuestEmail { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}