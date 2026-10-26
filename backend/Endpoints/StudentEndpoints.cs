using Microsoft.EntityFrameworkCore;
using static EndpointIdentity;

static class StudentEndpoints
{
    public static void MapStudentEndpoints(this WebApplication app)
    {
        app.MapGet("/api/student", async (HttpContext ctx, IStudentBenefits benefits) =>
            Results.Ok(new { status = await benefits.GetStatusAsync(GetOwnerId(ctx)) })).RequireAuthorization(Permissions.CanManageOwnStudentApplication);
        app.MapPost("/api/student", async (HttpContext ctx, Store db, IDocumentStorage storage) =>
        {
            if (!ctx.Request.HasFormContentType)
                return Results.BadRequest(new
                {
                    error = "Belge gerekli."
                });
            const long maxFileSize = 5 * 1024 * 1024;
            const long maxRequestSize = maxFileSize + 64 * 1024;
            if (ctx.Request.ContentLength > maxRequestSize)
                return Results.Json(new { error = "Belge en fazla 5 MB olabilir." }, statusCode: 413);
            var sizeFeature = ctx.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>();
            if (sizeFeature is { IsReadOnly: false })
                sizeFeature.MaxRequestBodySize = maxRequestSize;
            IFormCollection form;
            try
            {
                form = await ctx.Request.ReadFormAsync();
            }
            catch (InvalidDataException)
            {
                return Results.BadRequest(new { error = "Belge formu geçersiz veya 5 MB sınırını aşıyor." });
            }
            var file = form.Files.GetFile("document");
            if (file is not null && file.Length > maxFileSize)
                return Results.Json(new { error = "Belge en fazla 5 MB olabilir." }, statusCode: 413);
            if (file is null || file.Length < 8)
                return Results.BadRequest(new
                {
                    error = "PDF, PNG veya JPEG belge yükleyin (en fazla 5 MB)."
                });
            var ownerId = GetOwnerId(ctx);
            if (await db.Documents.AnyAsync(d => d.Session == ownerId && d.Status == "pending"))
                return Results.Conflict(new
                {
                    error = "Başvurunuz zaten incelemede."
                });
            using var content = new MemoryStream();
            await file.CopyToAsync(content);
            var bytes = content.ToArray();
            var ext = storage.DetectExtension(bytes);
            if (ext is null)
                return Results.BadRequest(new
                {
                    error = "Dosyanın içeriği PDF, PNG veya JPEG olmalı."
                });
            var id = Guid.NewGuid().ToString("N");
            await storage.SaveAsync(id, ext, bytes);
            db.Documents.Add(new StudentDocument { Id = id, Session = ownerId, Extension = ext });
            await db.SaveChangesAsync();
            return Results.Ok(new
            {
                status = "pending"
            });
        }).DisableAntiforgery().RequireAuthorization(Permissions.CanManageOwnStudentApplication).RequireRateLimiting("upload");
    }
}
