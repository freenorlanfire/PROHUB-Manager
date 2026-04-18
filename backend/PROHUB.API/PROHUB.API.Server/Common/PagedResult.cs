namespace PROHUB.API.Server.Common;

public record PagedResult<T>(
    List<T> Items,
    int Page,
    int PageSize,
    int Total)
{
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrev => Page > 1;
}
