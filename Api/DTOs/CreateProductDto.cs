using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

public class CreateProductDto
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 1000000)]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int Stock { get; set; }

    public string? ImageUrl { get; set; }

    [Required]
    public int CategoryId { get; set; }
}