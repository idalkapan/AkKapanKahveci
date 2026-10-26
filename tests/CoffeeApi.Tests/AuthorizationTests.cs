using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class AuthorizationTests
{
    [Fact]
    public async Task EveryPermissionMatchesTheRoleMatrix()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(PermissionPolicies.Configure);
        using var provider = services.BuildServiceProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        string[] customerPermissions = [Permissions.CanPlaceOrder, Permissions.CanViewOwnOrders,
            Permissions.CanManageOwnStudentApplication, Permissions.CanCancelOwnOrder];
        var permissions = typeof(Permissions).GetFields().Select(field => (string)field.GetRawConstantValue()!);
        foreach (var permission in permissions)
        {
            Assert.False((await authorization.AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()), null, permission)).Succeeded);
            foreach (var role in Enum.GetValues<UserRole>())
            {
                var account = new Account { Roles = [new() { Role = role }] };
                var expected = role == UserRole.Admin ||
                    role == UserRole.Customer && customerPermissions.Contains(permission) ||
                    role == UserRole.Barista && permission == Permissions.CanManageOrders;
                Assert.Equal(expected, (await authorization.AuthorizeAsync(AccountPrincipal.Create(account), null, permission)).Succeeded);
            }
        }
    }

    [Fact]
    public void PrincipalSupportsMultipleRolesWithoutLosingAccountIdentity()
    {
        var account = new Account { Id = "account-id", Roles = [new() { Role = UserRole.Admin }, new() { Role = UserRole.Barista }] };
        var principal = AccountPrincipal.Create(account);
        Assert.True(principal.IsInRole(nameof(UserRole.Admin)));
        Assert.True(principal.IsInRole(nameof(UserRole.Barista)));
        Assert.Equal(account.Id, principal.FindFirstValue(ClaimTypes.NameIdentifier));
    }
}
