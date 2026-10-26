using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Threading.RateLimiting;


static class ServiceRegistration
{
    public static IServiceCollection AddCoffeeServices(this IServiceCollection services)
    {
        Directory.CreateDirectory("data/documents");
        services.AddDbContext<Store>(o => o.UseSqlite("Data Source=data/coffee.db"));
        services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o => o.MultipartBodyLengthLimit = 5 * 1024 * 1024 + 64 * 1024);
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o =>
        {
            o.Cookie.Name = "coffee-auth";
            o.Cookie.HttpOnly = true;
            o.Cookie.SameSite = SameSiteMode.Lax;
            o.ExpireTimeSpan = TimeSpan.FromDays(7);
            o.SlidingExpiration = true;
            o.EventsType = typeof(AccountCookieEvents);
        });
        services.AddScoped<AccountCookieEvents>();
        services.AddScoped<AuthService>();
        services.AddAuthorization(PermissionPolicies.Configure);
        services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = true);
        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = 429;
            o.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = "60";
                await context.HttpContext.Response.WriteAsJsonAsync(new { error = "Çok fazla istek gönderdiniz. Bir dakika sonra tekrar deneyin." }, cancellationToken);
            };
            o.AddPolicy("checkout", ctx => RateLimitPartition.GetFixedWindowLimiter(
                ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "local",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            o.AddPolicy("upload", ctx => RateLimitPartition.GetFixedWindowLimiter(
                ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "local",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions { PermitLimit = 15, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IOrderPricing, OrderPricing>();
        services.AddScoped<IStudentBenefits, StudentBenefits>();
        services.AddSingleton<IDocumentStorage, LocalDocumentStorage>();
        return services;
    }
}
