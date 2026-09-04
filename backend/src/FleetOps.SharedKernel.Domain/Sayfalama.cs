namespace FleetOps.SharedKernel.Domain;

// Sayfa parametreleri TEK yerde sinirlaniyor. pageSize sinirsiz birakilirsa
// ?pageSize=1000000 tek istekle tum tabloyu bellege ceker; bu bir DoS yoludur.
public sealed record PageRequest
{
    public const int VarsayilanBoyut = 20;
    public const int AzamiBoyut = 100;

    public PageRequest(int? page, int? pageSize)
    {
        Page = page is null or < 1 ? 1 : page.Value;
        PageSize = pageSize is null or < 1
            ? VarsayilanBoyut
            : Math.Min(pageSize.Value, AzamiBoyut);
    }

    public int Page { get; }

    public int PageSize { get; }

    public int Atlanacak => (Page - 1) * PageSize;
}

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => TotalCount == 0
        ? 0
        : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasNext => Page < TotalPages;
}
