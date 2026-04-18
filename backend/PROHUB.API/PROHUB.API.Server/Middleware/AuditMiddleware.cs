using PROHUB.API.Server.Data;
using PROHUB.API.Server.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PROHUB.API.Server.Middleware;

public static class AuditMiddlewareExtensions
{
    public static IApplicationBuilder UseAuditLogging(this IApplicationBuilder app)
        => app.Use(async (ctx, next) =>
        {
            await next();

            // Only log successful mutations
            if (ctx.Request.Method is "POST" or "PUT" or "DELETE"
                && ctx.Response.StatusCode is >= 200 and < 300
                && ctx.Request.Path.StartsWithSegments("/api")
                && !ctx.Request.Path.StartsWithSegments("/api/auth"))
            {
                try
                {
                    await using var scope = ctx.RequestServices.CreateAsyncScope();
                    var db = scope.ServiceProvider.GetRequiredService<ProhubDbContext>();

                    Guid? userId = null;
                    if (ctx.User.Identity?.IsAuthenticated == true)
                    {
                        var sub = ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
                        if (Guid.TryParse(sub, out var uid)) userId = uid;
                    }

                    db.AuditLogs.Add(new AuditLog
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        EntityType = ctx.Request.Path.ToString(),
                        Action = ctx.Request.Method,
                        Details = $"Status:{ctx.Response.StatusCode}",
                        CreatedAtUtc = DateTime.UtcNow
                    });
                    await db.SaveChangesAsync();
                }
                catch { /* never crash on audit */ }
            }
        });
}
