using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Data;

namespace Api.Controllers;

// Not just Admin-gated: every action here also checks the hosting environment and
// 404s outright in Production, so this can never be reachable on a real deployment
// no matter what calls it or with what token.
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class DebugController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHostEnvironment _environment;

    public DebugController(AppDbContext context, IHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    // DELETE: api/debug/reset-orders
    // Clears Orders and OrderItems only - the catalog (Products/Categories) is untouched.
    [HttpDelete("reset-orders")]
    public async Task<IActionResult> ResetOrders()
    {
        if (_environment.IsProduction())
            return NotFound();

        await _context.OrderItems.ExecuteDeleteAsync();
        await _context.Orders.ExecuteDeleteAsync();

        return NoContent();
    }
}
