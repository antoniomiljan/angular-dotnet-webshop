namespace Api.Models;

// Not read or written by any endpoint. The active cart is client-side only
// (CartService + localStorage in the Angular app).
public class Cart
{
    public int Id { get; set; }

    public Guid? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}