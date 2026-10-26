enum UserRole
{
    Customer,
    Barista,
    Admin
}

sealed class AccountRole
{
    public string UserId { get; set; } = "";
    public UserRole Role { get; set; }
}
