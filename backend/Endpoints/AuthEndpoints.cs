using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using static EndpointIdentity;

static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/register", async (Register input, HttpContext ctx, AuthService service) =>
            await CompleteSignIn(await service.RegisterAsync(input), ctx)).RequireRateLimiting("auth");
        app.MapPost("/api/auth/login", async (Login input, HttpContext ctx, AuthService service) =>
            await CompleteSignIn(await service.LoginAsync(input), ctx)).RequireRateLimiting("auth");
        app.MapGet("/api/auth/me", (HttpContext ctx) => Results.Ok(new
        {
            name = ctx.User.Identity!.Name,
            email = ctx.User.FindFirstValue(ClaimTypes.Email),
            roles = ctx.User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray()
        })).RequireAuthorization();
        app.MapPost("/api/auth/logout", async (HttpContext ctx) =>
        {
            await ctx.SignOutAsync();
            return Results.Ok(new { status = "ok" });
        });
    }

    private static async Task<IResult> CompleteSignIn(AuthResult result, HttpContext ctx)
    {
        if (result.Account is not { } account)
            return Results.Json(new { error = result.Error }, statusCode: result.StatusCode);
        await SignIn(ctx, account);
        return Results.Ok(new { account.Name, account.Email, roles = account.Roles.Select(role => role.Role.ToString()) });
    }
}
