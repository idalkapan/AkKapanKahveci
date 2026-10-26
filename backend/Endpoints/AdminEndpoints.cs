using Microsoft.EntityFrameworkCore;

static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/documents").RequireAuthorization(Permissions.CanReviewStudentDocuments);
        group.MapGet("", async (Store db) => Results.Ok(await db.Documents.Select(d => new { d.Id, d.Status, d.Created }).ToListAsync()));
        group.MapGet("/{id}/file", async (string id, Store db, IDocumentStorage storage) =>
        {
            var d = await db.Documents.FindAsync(id);
            return d is null ? Results.NotFound() : Results.File(storage.GetPath(d.Id, d.Extension), d.Extension == ".pdf" ? "application/pdf" : d.Extension == ".png" ? "image/png" : "image/jpeg", "student" + d.Extension);
        });
        group.MapPost("/{id}/review", async (string id, Review review, Store db) =>
        {
            var d = await db.Documents.FindAsync(id);
            if (d is null)
                return Results.NotFound();
            if (review.Status is not ("approved" or "rejected"))
                return Results.BadRequest();
            d.Status = review.Status;
            await db.SaveChangesAsync();
            return Results.Ok(new
            {
                d.Status
            });
        });
    }
}
