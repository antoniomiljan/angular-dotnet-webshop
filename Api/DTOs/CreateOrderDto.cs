using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

public class CreateOrderItemDto
{
    [Required]
    public int ProductId { get; set; }

    [Range(1, 1000)]
    public int Quantity { get; set; }
}

public class CreateOrderDto
{
    [Required, MinLength(1)]
    public List<CreateOrderItemDto> Items { get; set; } = new();

    [EmailAddress]
    public string? GuestEmail { get; set; }
}