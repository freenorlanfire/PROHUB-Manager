namespace PROHUB.API.Server.Dtos;

public record CompanyDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? LogoUrl,
    string? WebsiteUrl,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);

public record CreateCompanyRequest(
    string Name,
    string? Description,
    string? LogoUrl,
    string? WebsiteUrl
);

public record UpdateCompanyRequest(
    string Name,
    string? Description,
    string? LogoUrl,
    string? WebsiteUrl
);
