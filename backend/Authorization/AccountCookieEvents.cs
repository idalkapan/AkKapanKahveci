using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

sealed class AccountCookieEvents(Store store) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var id = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var account = await store.Users.AsNoTracking().Include(user => user.Roles)
            .SingleOrDefaultAsync(user => user.Id == id, context.HttpContext.RequestAborted);
        if (account is null)
        {
            context.RejectPrincipal();
            return;
        }
        // Never trust role claims cached in a previously issued cookie.
        context.ReplacePrincipal(AccountPrincipal.Create(account));
    }

    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return context.Response.WriteAsJsonAsync(new { error = "Devam etmek için giriş yapın." });
    }

    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return context.Response.WriteAsJsonAsync(new { error = "Bu işlem için yetkiniz bulunmuyor." });
    }
}
