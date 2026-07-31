using Microsoft.AspNetCore.Identity;

namespace Api.Models;

public class ApplicationUser : IdentityUser<Guid>
{
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public Cart? Cart { get; set; }
}