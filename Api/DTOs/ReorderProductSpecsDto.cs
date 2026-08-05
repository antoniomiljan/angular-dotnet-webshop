using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

public class ReorderProductSpecsDto
{
    [Required, MinLength(1)]
    public List<int> SpecIds { get; set; } = new();
}
