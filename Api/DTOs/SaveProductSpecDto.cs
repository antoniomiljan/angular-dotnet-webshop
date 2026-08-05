using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

// Shared shape for both adding a new spec and editing an existing one's text.
public class SaveProductSpecDto
{
    [Required, MaxLength(100)]
    public string Label { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Value { get; set; } = string.Empty;
}
