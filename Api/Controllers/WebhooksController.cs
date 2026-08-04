using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Api.Data;
using Api.Models;

namespace Api.Controllers;

[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    public WebhooksController(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

[HttpPost("stripe")]
public async Task<IActionResult> StripeWebhook()
{
    var json = await new StreamReader(Request.Body).ReadToEndAsync();
    var webhookSecret = _config["Stripe:WebhookSecret"];

    if (!Request.Headers.TryGetValue("Stripe-Signature", out var signatureHeader))
        return BadRequest("Missing Stripe-Signature header.");

    Event stripeEvent;
    try
    {
        stripeEvent = EventUtility.ConstructEvent(
            json,
            signatureHeader,
            webhookSecret);
    }
    catch (StripeException e)
    {
        return BadRequest($"Webhook signature verification failed: {e.Message}");
    }

    if (stripeEvent.Type == "payment_intent.succeeded")
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.StripePaymentIntentId == paymentIntent!.Id);

        // Stripe can deliver the same event more than once, and the order may already
        // have moved past Pending (e.g. an admin already actioned it) - only advance
        // from Pending so a replayed event can't revert a later status.
        if (order is not null && order.Status == OrderStatus.Pending)
        {
            order.Status = OrderStatus.Paid;
            await _context.SaveChangesAsync();
        }
    }

    return Ok();
}
}