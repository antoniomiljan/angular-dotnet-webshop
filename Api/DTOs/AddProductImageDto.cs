using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

public class AddProductImageDto
{
    [Required]
    public string ImageUrl { get; set; } = string.Empty;
}
