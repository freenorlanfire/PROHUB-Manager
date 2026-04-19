namespace PROHUB.API.Server.Dtos;

public record TrendItem(
    string Id,
    string Source,        // "hn" | "devto" | "github"
    string Title,
    string Url,
    string? Description,
    string? Author,
    int     Points,
    string  PublishedAt,
    string[] Tags
);

public record TrendsResult(
    List<TrendItem> Items,
    string[]        QueryTags,
    string          FetchedAtUtc
);

public record AiEnhanceRequest(
    string? ExtraInstructions
);

public record AiEnhanceResponse(
    string Content,
    string Model,
    bool   WasAiGenerated
);
