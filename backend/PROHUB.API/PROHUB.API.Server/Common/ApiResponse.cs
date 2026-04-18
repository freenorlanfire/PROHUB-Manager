namespace PROHUB.API.Server.Common;

/// <summary>Standard API envelope: { ok, data, error }</summary>
public record ApiResponse<T>(bool Ok, T? Data, string? Error)
{
    public static ApiResponse<T> Success(T data) => new(true, data, null);
    public static ApiResponse<T> Fail(string error) => new(false, default, error);
}

public record ApiResponse(bool Ok, object? Data, string? Error)
{
    public static ApiResponse Success(object? data = null) => new(true, data, null);
    public static ApiResponse Fail(string error) => new(false, null, error);
}
