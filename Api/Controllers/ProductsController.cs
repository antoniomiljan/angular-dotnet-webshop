using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Api.Data;
using Api.Models;
using Api.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly IWebHostEnvironment _environment;

    public ProductsController(AppDbContext context, IMapper mapper, IWebHostEnvironment environment)
    {
        _context = context;
        _mapper = mapper;
        _environment = environment;
    }

    // GET: api/products?categoryId=1&search=shirt
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts(
        [FromQuery] int? categoryId,
        [FromQuery] string? search)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Images.OrderBy(i => i.SortOrder))
            .Where(p => p.IsActive)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(normalizedSearch));
        }

        var products = await query.ToListAsync();
        return Ok(_mapper.Map<List<ProductDto>>(products));
    }

    // GET: api/products/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetProduct(int id)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Images.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

        if (product is null)
            return NotFound();

        return Ok(_mapper.Map<ProductDto>(product));
    }

    // POST: api/products
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<ProductDto>> CreateProduct(CreateProductDto dto)
    {
        var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId);
        if (!categoryExists)
            return BadRequest($"Category {dto.CategoryId} does not exist.");

        var product = _mapper.Map<Product>(dto);
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        await _context.Entry(product).Reference(p => p.Category).LoadAsync();
        var resultDto = _mapper.Map<ProductDto>(product);

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, resultDto);
    }

    // PUT: api/products/5
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, UpdateProductDto dto)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null)
            return NotFound();

        var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId);
        if (!categoryExists)
            return BadRequest($"Category {dto.CategoryId} does not exist.");

        _mapper.Map(dto, product);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/products/5 (soft delete)
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null)
            return NotFound();

        product.IsActive = false;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // POST: api/products/5/images
    // Attaches an already-uploaded image (see UploadsController) to a product, appended
    // at the end of its current image order.
    [Authorize(Roles = "Admin")]
    [HttpPost("{id}/images")]
    public async Task<ActionResult<ProductImageDto>> AddImage(int id, AddProductImageDto dto)
    {
        var product = await _context.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
            return NotFound();

        var nextSortOrder = product.Images.Count == 0 ? 0 : product.Images.Max(i => i.SortOrder) + 1;
        var image = new ProductImage
        {
            ImageUrl = dto.ImageUrl,
            SortOrder = nextSortOrder,
            ProductId = product.Id
        };

        _context.ProductImages.Add(image);
        await _context.SaveChangesAsync();

        return Ok(_mapper.Map<ProductImageDto>(image));
    }

    // DELETE: api/products/5/images/10
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}/images/{imageId}")]
    public async Task<IActionResult> RemoveImage(int id, int imageId)
    {
        var image = await _context.ProductImages
            .FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == id);
        if (image is null)
            return NotFound();

        _context.ProductImages.Remove(image);
        await _context.SaveChangesAsync();

        // Only ever deletes a file this app served itself (under wwwroot/images) - an
        // externally-hosted image URL has nothing local to clean up. GetFileName also
        // strips any directory components, so this can't escape the images folder.
        if (image.ImageUrl.StartsWith("/images/"))
        {
            var fileName = Path.GetFileName(image.ImageUrl);
            var filePath = Path.Combine(_environment.WebRootPath, "images", fileName);
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }

        return NoContent();
    }

    // PUT: api/products/5/images/reorder
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}/images/reorder")]
    public async Task<IActionResult> ReorderImages(int id, ReorderProductImagesDto dto)
    {
        var images = await _context.ProductImages
            .Where(i => i.ProductId == id)
            .ToListAsync();

        if (dto.ImageIds.Count != images.Count || !dto.ImageIds.All(imgId => images.Any(i => i.Id == imgId)))
            return BadRequest("The provided image ids must exactly match the product's current images.");

        for (var index = 0; index < dto.ImageIds.Count; index++)
        {
            var image = images.First(i => i.Id == dto.ImageIds[index]);
            image.SortOrder = index;
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }
}