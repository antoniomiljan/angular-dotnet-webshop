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
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public CategoriesController(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    // GET: api/categories
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
    {
        var categories = await _context.Categories.ToListAsync();
        return Ok(_mapper.Map<List<CategoryDto>>(categories));
    }

    // GET: api/categories/5
    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryDto>> GetCategory(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category is null)
            return NotFound();

        return Ok(_mapper.Map<CategoryDto>(category));
    }

    // POST: api/categories
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<CategoryDto>> CreateCategory(CreateCategoryDto dto)
    {
        var slugExists = await _context.Categories.AnyAsync(c => c.Slug == dto.Slug);
        if (slugExists)
            return BadRequest($"A category with slug '{dto.Slug}' already exists.");

        var category = _mapper.Map<Category>(dto);
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var resultDto = _mapper.Map<CategoryDto>(category);
        return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, resultDto);
    }
}