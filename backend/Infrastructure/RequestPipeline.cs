static class RequestPipeline
{
    public static void UseCoffeePipeline(this WebApplication app)
    {
        app.Use(async (ctx, next) =>
        {
            if (ctx.Request.Path.StartsWithSegments("/api"))
                ctx.Response.Headers.CacheControl = "no-store";
            if (ctx.Request.Method is not ("GET" or "HEAD" or "OPTIONS") && ctx.Request.Headers["Sec-Fetch-Site"] == "cross-site")
            {
                ctx.Response.StatusCode = 403;
                await ctx.Response.WriteAsJsonAsync(new { error = "Bu kaynaktan istek kabul edilmiyor." });
                return;
            }
            try
            {
                await next();
            }
            catch (BadHttpRequestException exception) when (!ctx.Response.HasStarted)
            {
                ctx.Response.StatusCode = exception.StatusCode == 413 ? 413 : 400;
                await ctx.Response.WriteAsJsonAsync(new { error = exception.StatusCode == 413 ? "İstek boyutu izin verilen sınırı aşıyor." : "Geçersiz istek." });
            }
        });
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();
    }
}
