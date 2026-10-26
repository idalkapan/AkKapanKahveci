using Microsoft.AspNetCore.Authorization;

static class PermissionPolicies
{
    // Menu/content reads remain public. All protected role mappings live here.
    public static void Configure(AuthorizationOptions options)
    {
        Add(options, Permissions.CanPlaceOrder, UserRole.Customer, UserRole.Admin);
        Add(options, Permissions.CanViewOwnOrders, UserRole.Customer, UserRole.Admin);
        Add(options, Permissions.CanManageOwnStudentApplication, UserRole.Customer, UserRole.Admin);
        Add(options, Permissions.CanCancelOwnOrder, UserRole.Customer, UserRole.Admin);
        Add(options, Permissions.CanManageOrders, UserRole.Barista, UserRole.Admin);
        Add(options, Permissions.CanManageCatalog, UserRole.Admin);
        Add(options, Permissions.CanManageCampaigns, UserRole.Admin);
        Add(options, Permissions.CanReviewStudentDocuments, UserRole.Admin);
        Add(options, Permissions.CanManageContent, UserRole.Admin);
        Add(options, Permissions.CanManageStaff, UserRole.Admin);
    }

    private static void Add(AuthorizationOptions options, string name, params UserRole[] roles) =>
        options.AddPolicy(name, policy => policy.RequireAuthenticatedUser()
            .RequireRole(roles.Select(role => role.ToString()).ToArray()));
}
