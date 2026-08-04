using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UploadsController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;

    // Extension-only rather than a hard-coded MIME table: browsers are inconsistent
    // about what Content-Type they send for a given file, and this is validated
    // against an authenticated admin's own file, not arbitrary public input.
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    public UploadsController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    // POST: api/uploads/image
    [HttpPost("image")]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> UploadImage(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file provided.");

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
            return BadRequest("Unsupported file type. Use JPG, PNG, WEBP, or GIF.");

        // Random filename: never trust the client-supplied name for a path, and it
        // sidesteps collisions between products with similarly-named source files.
        var fileName = $"{Guid.NewGuid()}{extension.ToLowerInvariant()}";
        var imagesDir = Path.Combine(_environment.WebRootPath, "images");
        Directory.CreateDirectory(imagesDir);

        await using (var stream = System.IO.File.Create(Path.Combine(imagesDir, fileName)))
        {
            await file.CopyToAsync(stream);
        }

        return Ok(new { imageUrl = $"/images/{fileName}" });
    }
}
