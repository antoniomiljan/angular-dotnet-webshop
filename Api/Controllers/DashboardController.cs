using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Data;
using Api.DTOs;
using Api.Models;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;
    private const int LowStockThreshold = 10;

    // Statuses that represent an actual completed sale, not just a placed order.
    private static readonly OrderStatus[] RevenueStatuses =
        { OrderStatus.Paid, OrderStatus.Shipped, OrderStatus.Delivered };

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/dashboard
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> GetDashboard()
    {
        var totalRevenue = await _context.Orders
            .Where(o => RevenueStatuses.Contains(o.Status))
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

        var totalOrders = await _context.Orders.CountAsync();
        var pendingOrdersCount = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending);
        var activeProductsCount = await _context.Products.CountAsync(p => p.IsActive);

        var lowStockProducts = await _context.Products
            .Where(p => p.IsActive && p.Stock <= LowStockThreshold)
            .OrderBy(p => p.Stock)
            .Select(p => new LowStockProductDto { Id = p.Id, Name = p.Name, Stock = p.Stock })
            .ToListAsync();

        var topProducts = await _context.OrderItems
            .Where(oi => RevenueStatuses.Contains(oi.Order.Status))
            .GroupBy(oi => new { oi.ProductId, oi.Product.Name })
            .Select(g => new TopProductDto
            {
                ProductId = g.Key.ProductId,
                Name = g.Key.Name,
                QuantitySold = g.Sum(oi => oi.Quantity),
                Revenue = g.Sum(oi => oi.Quantity * oi.UnitPriceAtPurchase)
            })
            .OrderByDescending(p => p.QuantitySold)
            .Take(5)
            .ToListAsync();

        return Ok(new DashboardDto
        {
            TotalRevenue = totalRevenue,
            TotalOrders = totalOrders,
            PendingOrdersCount = pendingOrdersCount,
            ActiveProductsCount = activeProductsCount,
            LowStockProducts = lowStockProducts,
            TopProducts = topProducts
        });
    }
}
