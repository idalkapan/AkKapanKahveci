using System.Text.Json;

using static EndpointIdentity;

static class CheckoutEndpoints
{
    public static void MapCheckoutEndpoints(this WebApplication app)
    {
        app.MapPost("/api/checkout", async (Checkout request, HttpContext ctx, Store db, IOrderPricing pricing, IStudentBenefits benefits, IConfiguration configuration) =>
        {
            if (configuration["Payments:Mode"] != "Demo")
                return Results.Json(new
                {
                    error = "Canlı ödeme sağlayıcısı henüz yapılandırılmadı."
                }, statusCode: 503);
            if (request.Items is null || request.Items.Count is < 1 or > 50 || string.IsNullOrWhiteSpace(request.Customer) || request.Customer.Length > 80 || request.Concept is not ("candy" or "forest" or "sunset"))
                return Results.BadRequest(new
                {
                    error = "Sipariş bilgileri geçersiz."
                });
            var session = GetOwnerId(ctx);
            var price = pricing.Calculate(request.Items);
            if (price is null)
                return Results.BadRequest(new
                {
                    error = "Sepet seçimi geçersiz."
                });
            var discount = await benefits.CalculateDiscountAsync(session, price.Subtotal);
            var order = new Order { Session = session, Customer = request.Customer.Trim(), Total = price.Subtotal + price.CupFee - discount, Discount = discount, ItemsJson = JsonSerializer.Serialize(request.Items), Concept = request.Concept };
            db.Orders.Add(order);
            await db.SaveChangesAsync();
            return Results.Ok(new
            {
                order.Id,
                order.Total,
                order.Discount,
                status = "demo-paid",
                message = "Demo sipariş tamamlandı. Gerçek ücret alınmadı."
            });
        }).RequireAuthorization(Permissions.CanPlaceOrder).RequireRateLimiting("checkout");
    }
}
