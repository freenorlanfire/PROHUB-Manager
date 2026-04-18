namespace PROHUB.API.Server.Common;

public static class AppErrors
{
    public static string NotFound(string entity) => $"{entity} not found.";
    public static string Conflict(string msg) => msg;
    public const string InvalidId = "Invalid ID format.";
    public const string CompanyRequired = "X-Company-Id header or companyId query parameter is required.";
    public const string BadRequest = "Invalid request body.";
}
