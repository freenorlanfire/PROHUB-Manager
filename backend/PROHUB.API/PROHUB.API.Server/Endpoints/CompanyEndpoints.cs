using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PROHUB.API.Server.Common;
using PROHUB.API.Server.Data;
using PROHUB.API.Server.Dtos;
using PROHUB.API.Server.Entities;

namespace PROHUB.API.Server.Endpoints;

public static class CompanyEndpoints
{
    public static IEndpointRouteBuilder MapCompanyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/companies")
            .WithTags("Companies")
            .RequireAuthorization();

        group.MapGet("/", GetCompanies);
        group.MapPost("/", CreateCompany);
        group.MapGet("/{id:guid}", GetCompany);
        group.MapPut("/{id:guid}", UpdateCompany);
        group.MapDelete("/{id:guid}", DeleteCompany);

        return app;
    }

    // GET /api/companies — only companies the user has membership in
    static async Task<IResult> GetCompanies(ProhubDbContext db, ClaimsPrincipal user)
    {
        var userId = GetUserId(user);
        var list = await db.Companies
            .Where(c => !c.IsDeleted &&
                        db.Memberships.Any(m => m.UserId == userId && m.CompanyId == c.Id))
            .OrderBy(c => c.Name)
            .Select(c => ToDto(c))
            .ToListAsync();

        return Results.Ok(ApiResponse<List<CompanyDto>>.Success(list));
    }

    // GET /api/companies/{id}
    static async Task<IResult> GetCompany(Guid id, ProhubDbContext db, ClaimsPrincipal user)
    {
        var userId = GetUserId(user);
        var c = await db.Companies.FirstOrDefaultAsync(x =>
            x.Id == id && !x.IsDeleted &&
            db.Memberships.Any(m => m.UserId == userId && m.CompanyId == x.Id));

        if (c is null) return Results.NotFound(ApiResponse.Fail(AppErrors.NotFound("Company")));
        return Results.Ok(ApiResponse<CompanyDto>.Success(ToDto(c)));
    }

    // POST /api/companies — creates company + owner membership for current user
    static async Task<IResult> CreateCompany(
        CreateCompanyRequest req, ProhubDbContext db, ClaimsPrincipal user)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return Results.BadRequest(ApiResponse.Fail("Name is required."));

        var userId = GetUserId(user);
        var now = DateTime.UtcNow;

        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = req.Name.Trim(),
            Slug = Slugify(req.Name),
            Description = req.Description?.Trim(),
            LogoUrl = req.LogoUrl?.Trim(),
            WebsiteUrl = req.WebsiteUrl?.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            IsDeleted = false
        };

        var membership = new Membership
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            UserId = userId,
            Role = "owner",
            CreatedAtUtc = now
        };

        db.Companies.Add(company);
        db.Memberships.Add(membership);
        await db.SaveChangesAsync();

        return Results.Created(
            $"/api/companies/{company.Id}",
            ApiResponse<CompanyDto>.Success(ToDto(company)));
    }

    // PUT /api/companies/{id}
    static async Task<IResult> UpdateCompany(
        Guid id, UpdateCompanyRequest req, ProhubDbContext db, ClaimsPrincipal user)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return Results.BadRequest(ApiResponse.Fail("Name is required."));

        var userId = GetUserId(user);
        var c = await db.Companies.FirstOrDefaultAsync(x =>
            x.Id == id && !x.IsDeleted &&
            db.Memberships.Any(m => m.UserId == userId && m.CompanyId == x.Id));

        if (c is null) return Results.NotFound(ApiResponse.Fail(AppErrors.NotFound("Company")));

        c.Name = req.Name.Trim();
        c.Slug = Slugify(req.Name);
        c.Description = req.Description?.Trim();
        c.LogoUrl = req.LogoUrl?.Trim();
        c.WebsiteUrl = req.WebsiteUrl?.Trim();
        c.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Results.Ok(ApiResponse<CompanyDto>.Success(ToDto(c)));
    }

    // DELETE /api/companies/{id}
    static async Task<IResult> DeleteCompany(Guid id, ProhubDbContext db, ClaimsPrincipal user)
    {
        var userId = GetUserId(user);
        var c = await db.Companies.FirstOrDefaultAsync(x =>
            x.Id == id && !x.IsDeleted &&
            db.Memberships.Any(m => m.UserId == userId && m.CompanyId == x.Id));

        if (c is null) return Results.NotFound(ApiResponse.Fail(AppErrors.NotFound("Company")));

        c.IsDeleted = true;
        c.DeletedAtUtc = DateTime.UtcNow;
        c.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(ApiResponse.Success());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    static Guid GetUserId(ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    static CompanyDto ToDto(Company c) =>
        new(c.Id, c.Name, c.Slug, c.Description, c.LogoUrl, c.WebsiteUrl,
            c.CreatedAtUtc, c.UpdatedAtUtc);

    static string Slugify(string name) =>
        System.Text.RegularExpressions.Regex.Replace(
            name.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
}
