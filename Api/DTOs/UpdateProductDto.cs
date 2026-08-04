using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

public class UpdateProductDto
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 1000000)]
    public decimal Price { get; set; }

    public bool InStock { get; set; } = true;

    [Required]
    public int CategoryId { get; set; }

    public bool IsActive { get; set; } = true;
}