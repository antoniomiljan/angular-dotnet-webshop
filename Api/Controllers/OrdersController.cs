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
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public OrdersController(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    // POST: api/orders
    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.GuestEmail))
            return BadRequest("GuestEmail is required until account login is implemented.");

        var productIds = dto.Items.Select(i => i.ProductId).ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id) && p.IsActive)
            .ToDictionaryAsync(p => p.Id);

        var missingIds = productIds.Except(products.Keys).ToList();
        if (missingIds.Any())
            return BadRequest($"Invalid or inactive product(s): {string.Join(", ", missingIds)}");

        var order = new Order
        {
            GuestEmail = dto.GuestEmail,
            Status = OrderStatus.Pending,
            Items = dto.Items.Select(item => new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPriceAtPurchase = products[item.ProductId].Price
            }).ToList()
        };

        order.TotalAmount = order.Items.Sum(i => i.Quantity * i.UnitPriceAtPurchase);

        await using var transaction = await _context.Database.BeginTransactionAsync();

        // Decrement stock atomically per row (WHERE Stock >= Quantity) so two concurrent
        // checkouts for the last unit can't both succeed - the DB, not this process, is the guard.
        foreach (var item in dto.Items)
        {
            var rowsAffected = await _context.Products
                .Where(p => p.Id == item.ProductId && p.Stock >= item.Quantity)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock - item.Quantity));

            if (rowsAffected == 0)
            {
                await transaction.RollbackAsync();
                return BadRequest($"Insufficient stock for '{products[item.ProductId].Name}'.");
            }
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        await _context.Entry(order).Collection(o => o.Items).Query()
            .Include(oi => oi.Product).LoadAsync();

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id, token = order.AccessToken }, _mapper.Map<OrderDto>(order));
    }

    // GET: api/orders/5?token=... (guest order lookup - token is the only proof of ownership, so a
    // wrong/missing token looks identical to a missing order rather than leaking that the id exists)
    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetOrder(int id, [FromQuery] Guid token)
    {
        var order = await _context.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null || order.AccessToken != token)
            return NotFound();

        return Ok(_mapper.Map<OrderDto>(order));
    }

    // POST: api/orders/5/cancel?token=... (guest self-cancel - only while still Pending, e.g. after
    // Stripe confirmation fails or never completes, so the stock reserved at creation isn't stuck forever)
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelOrder(int id, [FromQuery] Guid token)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null || order.AccessToken != token)
            return NotFound();

        if (order.Status != OrderStatus.Pending)
            return BadRequest("Only a pending order can be cancelled this way.");

        await using var transaction = await _context.Database.BeginTransactionAsync();

        foreach (var item in order.Items)
        {
            await _context.Products
                .Where(p => p.Id == item.ProductId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock + item.Quantity));
        }

        order.Status = OrderStatus.Cancelled;
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return NoContent();
    }

    // GET: api/orders (admin — list all orders)
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetAllOrders()
    {
        var orders = await _context.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return Ok(_mapper.Map<List<OrderDto>>(orders));
    }

    // PUT: api/orders/5/status
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int id, UpdateOrderStatusDto dto)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order is null)
            return NotFound();

        order.Status = dto.Status;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
