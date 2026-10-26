using Microsoft.EntityFrameworkCore;

interface IStudentBenefits
{
    Task<string> GetStatusAsync(string ownerId);
    Task<decimal> CalculateDiscountAsync(string ownerId, decimal subtotal);
}

sealed class StudentBenefits(Store store) : IStudentBenefits
{
    public async Task<string> GetStatusAsync(string ownerId)
    {
        var document = await store.Documents
            .Where(document => document.Session == ownerId)
            .OrderByDescending(document => document.Created)
            .FirstOrDefaultAsync();

        if (document is null)
            return "none";
        if (document.Status == "approved" && document.Created <= DateTime.UtcNow.AddYears(-1))
            return "expired";
        return document.Status;
    }

    public async Task<decimal> CalculateDiscountAsync(string ownerId, decimal subtotal) =>
        await GetStatusAsync(ownerId) == "approved" ? decimal.Round(subtotal * .1m, 2) : 0;
}
