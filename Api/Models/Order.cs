namespace Api.Models;

public class Order
{
    public int Id { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? StripePaymentIntentId { get; set; }

    // Proof of ownership for guest order lookups. Prevents enumerating orders via sequential ids.
    public Guid AccessToken { get; set; } = Guid.NewGuid();

    public Guid? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public string? GuestEmail { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}