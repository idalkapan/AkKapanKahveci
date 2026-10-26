using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;

static class AccountPrincipal
{
    public static ClaimsPrincipal Create(Account account)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.Id),
            new(ClaimTypes.Name, account.Name),
            new(ClaimTypes.Email, account.Email)
        };
        claims.AddRange(account.Roles.Select(role => new Claim(ClaimTypes.Role, role.Role.ToString())));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }
}
