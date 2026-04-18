using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PROHUB.API.Server.Common;
using PROHUB.API.Server.Data;
using PROHUB.API.Server.Dtos;
using PROHUB.API.Server.Entities;
using PROHUB.API.Server.Services;

namespace PROHUB.API.Server.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", Login);
        group.MapPost("/refresh", Refresh);
        group.MapPost("/logout", Logout);
        group.MapGet("/me", Me).RequireAuthorization();

        return app;
    }

    // ── POST /api/auth/login ──────────────────────────────────────────────────────

    static async Task<IResult> Login(
        LoginRequest req,
        ProhubDbContext db,
        AuthService auth)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return Results.BadRequest(ApiResponse.Fail("Email and password are required."));

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == req.Email.Trim().ToLowerInvariant() && !u.IsDeleted);

        // constant-time: always verify even if user not found (dummy hash)
        var passwordOk = user is not null && auth.VerifyPassword(req.Password, user.PasswordHash);
        if (!passwordOk)
            return Results.BadRequest(ApiResponse.Fail("Invalid email or password."));

        var membership = await db.Memberships
            .FirstOrDefaultAsync(m => m.UserId == user!.Id);

        var (token, expiresAt) = auth.GenerateToken(user!, membership?.CompanyId);

        var rawRefresh = auth.GenerateRefreshToken();
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user!.Id,
            TokenHash = auth.HashToken(rawRefresh),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        return Results.Ok(ApiResponse<LoginResponse>.Success(new(
            token,
            rawRefresh,
            expiresAt,
            new(user.Id, user.Email, user.Name, user.Role),
            membership?.CompanyId
        )));
    }

    // ── POST /api/auth/refresh ────────────────────────────────────────────────────

    static async Task<IResult> Refresh(
        RefreshRequest req,
        ProhubDbContext db,
        AuthService auth)
    {
        if (string.IsNullOrWhiteSpace(req.RefreshToken))
            return Results.BadRequest(ApiResponse.Fail("Refresh token is required."));

        var hash = auth.HashToken(req.RefreshToken);

        var stored = await db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash && t.RevokedAtUtc == null);

        if (stored is null || stored.ExpiresAtUtc < DateTime.UtcNow || stored.User is null)
            return Results.Unauthorized();

        // Rotate: revoke old, issue new
        stored.RevokedAtUtc = DateTime.UtcNow;

        var membership = await db.Memberships
            .FirstOrDefaultAsync(m => m.UserId == stored.UserId);

        var (token, expiresAt) = auth.GenerateToken(stored.User, membership?.CompanyId);

        var newRaw = auth.GenerateRefreshToken();
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = stored.UserId,
            TokenHash = auth.HashToken(newRaw),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            CreatedAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        return Results.Ok(ApiResponse<RefreshResponse>.Success(
            new(token, newRaw, expiresAt)));
    }

    // ── POST /api/auth/logout ─────────────────────────────────────────────────────

    static async Task<IResult> Logout(LogoutRequest req, ProhubDbContext db, AuthService auth)
    {
        if (!string.IsNullOrWhiteSpace(req.RefreshToken))
        {
            var hash = auth.HashToken(req.RefreshToken);
            var stored = await db.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenHash == hash && t.RevokedAtUtc == null);

            if (stored is not null)
            {
                stored.RevokedAtUtc = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }
        }

        return Results.Ok(ApiResponse.Success());
    }

    // ── GET /api/auth/me ──────────────────────────────────────────────────────────

    static async Task<IResult> Me(ClaimsPrincipal user, ProhubDbContext db)
    {
        var userId = Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var u = await db.Users.FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted);
        if (u is null) return Results.Unauthorized();

        var membership = await db.Memberships.FirstOrDefaultAsync(m => m.UserId == userId);

        return Results.Ok(ApiResponse<LoginResponse>.Success(new(
            "", "", DateTime.UtcNow,
            new(u.Id, u.Email, u.Name, u.Role),
            membership?.CompanyId
        )));
    }
}
