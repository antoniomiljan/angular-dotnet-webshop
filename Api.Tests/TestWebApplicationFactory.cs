using Api.Data;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Api.Tests;

// Boots the real app against an isolated in-memory SQLite database instead of the
// dev Postgres instance, so tests need no external services and can't touch dev data.
// Program.cs seeds the Admin role during startup (before app.Run()), which runs against
// whatever DbContext is registered at that point - so the schema is created eagerly
// inside ConfigureServices, before the real host finishes building.
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _environmentName;
    private SqliteConnection? _connection;

    public TestWebApplicationFactory(string environmentName = "Testing")
    {
        _environmentName = environmentName;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environmentName);

        builder.UseSetting("Jwt:Key", "test-only-signing-key-32-chars-minimum-for-hmacsha256");
        builder.UseSetting("Jwt:Issuer", "WebshopApiTests");
        builder.UseSetting("Jwt:Audience", "WebshopClientTests");
        builder.UseSetting("Stripe:SecretKey", "sk_test_placeholder_not_a_real_key");

        builder.ConfigureServices(services =>
        {
            // AddDbContext<AppDbContext>(UseNpgsql) registers both DbContextOptions<AppDbContext>
            // and an IDbContextOptionsConfiguration<AppDbContext> holding the Npgsql configuration
            // action. Removing only the former leaves the latter in place, so both the Npgsql and
            // Sqlite provider configuration actions run - "Only a single database provider can be
            // registered" - unless both are stripped before re-registering with Sqlite.
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();

            // A single shared DbConnection object serializes every request through one
            // connection, which breaks genuinely concurrent requests (e.g. the stock-race
            // test): SQLite can't run overlapping transactions on one connection. Using a
            // shared-cache in-memory database by name instead lets each request open its
            // own connection, same as a real app would - _connection just holds one open
            // for the factory's lifetime so the database isn't dropped between requests.
            var dbName = $"testdb-{Guid.NewGuid():N}";
            var connectionString = $"Data Source=file:{dbName}?mode=memory&cache=shared";

            _connection = new SqliteConnection(connectionString);
            _connection.Open();

            services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        });
    }

    // Mints a real, validly-signed JWT without going through registration/role
    // assignment - JwtTokenService only needs an id/email and the roles to embed.
    public string CreateAdminToken()
    {
        using var scope = Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<JwtTokenService>();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "admin-test@example.com",
            UserName = "admin-test@example.com"
        };
        return tokenService.CreateToken(user, new List<string> { "Admin" });
    }

    public async Task<Category> SeedCategoryAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var category = new Category { Name = "Empty Category", Slug = Guid.NewGuid().ToString() };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return category;
    }

    public async Task<(Category Category, Product Product)> SeedProductAsync(
        bool inStock = true, decimal price = 10m, bool active = true, string name = "Test Product")
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var category = new Category { Name = "Test Category", Slug = Guid.NewGuid().ToString() };
        var product = new Product
        {
            Name = name,
            Description = "Test product for integration tests",
            Price = price,
            InStock = inStock,
            IsActive = active,
            Category = category
        };

        db.Categories.Add(category);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        return (category, product);
    }

    // Bypasses CreateOrder/Stripe entirely so tests can set up a specific order
    // status combination directly (dashboard stats need Pending/Paid/Cancelled mixes
    // that aren't all reachable through the normal checkout flow in a test).
    public async Task<Order> SeedOrderAsync(OrderStatus status, params (Product Product, int Quantity)[] items)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var order = new Order
        {
            Status = status,
            GuestEmail = "seed@example.com",
            Items = items.Select(i => new OrderItem
            {
                ProductId = i.Product.Id,
                Quantity = i.Quantity,
                UnitPriceAtPurchase = i.Product.Price
            }).ToList()
        };
        order.TotalAmount = order.Items.Sum(i => i.Quantity * i.UnitPriceAtPurchase);

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        return order;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection?.Dispose();
    }
}
