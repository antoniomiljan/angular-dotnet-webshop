using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

public class ReorderProductImagesDto
{
    [Required, MinLength(1)]
    public List<int> ImageIds { get; set; } = new();
}
