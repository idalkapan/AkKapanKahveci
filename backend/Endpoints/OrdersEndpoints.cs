using Microsoft.EntityFrameworkCore;
using static EndpointIdentity;

static class OrdersEndpoints
{
    public static void MapOrdersEndpoints(this WebApplication app)
    {
        app.MapGet("/api/orders", async (HttpContext ctx, Store db) => { var session = GetOwnerId(ctx); return Results.Ok(await db.Orders.Where(o => o.Session == session).OrderByDescending(o => o.Created).Select(o => new { o.Id, o.Total, o.Discount, o.Created, o.Concept }).ToListAsync()); }).RequireAuthorization(Permissions.CanViewOwnOrders);

    }
}
