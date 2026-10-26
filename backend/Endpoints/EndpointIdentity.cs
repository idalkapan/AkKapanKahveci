using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

static class EndpointIdentity
{
    public static string GetOwnerId(HttpContext ctx) =>
        ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Kimliği doğrulanmış kullanıcı gerekli.");

    public static Task SignIn(HttpContext ctx, Account user) =>
        ctx.SignInAsync(AccountPrincipal.Create(user), new AuthenticationProperties { IsPersistent = true });
}
