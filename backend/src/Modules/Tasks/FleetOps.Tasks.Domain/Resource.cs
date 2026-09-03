using FleetOps.SharedKernel.Domain;

namespace FleetOps.Tasks.Domain;

// Paylasilan tekil kaynak: sarj istasyonu, dar koridor, asansor.
// Kilitleri kendi icinde tutmuyor - "bir kaynagin tek aktif kilidi olur"
// kurali satirlar arasi bir kural ve veritabaninda korunuyor.
public sealed class Resource : AggregateRoot
{
    private Resource(Guid id, string code, ResourceKind kind) : base(id)
    {
        Code = code;
        Kind = kind;
    }

    private Resource()
    {
        Code = string.Empty;
    }

    public string Code { get; private set; }

    public ResourceKind Kind { get; private set; }

    public static Result<Resource> Create(Guid id, string code, ResourceKind kind) =>
        string.IsNullOrWhiteSpace(code)
            ? Result.Failure<Resource>(ResourceErrors.KodBos)
            : Result.Success(new Resource(id, code.Trim(), kind));
}
