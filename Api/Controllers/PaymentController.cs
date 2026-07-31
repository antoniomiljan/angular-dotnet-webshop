using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Api.Data;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PaymentsController(AppDbContext context)
    {
        _context = context;
    }

    // POST: api/payments/create-intent/5
    [HttpPost("create-intent/{orderId}")]
    public async Task<IActionResult> CreatePaymentIntent(int orderId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order is null)
            return NotFound();

        if (order.Status != Models.OrderStatus.Pending)
            return BadRequest("This order is not in a payable state.");

        var amountInCents = (long)(order.TotalAmount * 100);

        var options = new PaymentIntentCreateOptions
        {
            Amount = amountInCents,
            Currency = "usd",
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
                AllowRedirects = "never",
            },
            Metadata = new Dictionary<string, string>
            {
                { "order_id", order.Id.ToString() }
            }
        };

        var service = new PaymentIntentService();
        var intent = await service.CreateAsync(options);

        order.StripePaymentIntentId = intent.Id;
        await _context.SaveChangesAsync();

        return Ok(new { clientSecret = intent.ClientSecret });
    }
}