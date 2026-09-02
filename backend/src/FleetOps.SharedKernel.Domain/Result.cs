namespace FleetOps.SharedKernel.Domain;

/// <summary>Beklenen bir is hatasi. Beklenmeyen hatalar exception olarak kalir.</summary>
public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
}

/// <summary>
/// Beklenen is hatalarini exception firlatmadan dondurmek icin.
/// "AGV musait degil" bir hata degil, gecerli bir sonuctur; exception
/// akis kontrolu icin kullanilmaz.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new ArgumentException("Basarili sonuc hata tasiyamaz.", nameof(error));
        }

        if (!isSuccess && error == Error.None)
        {
            throw new ArgumentException("Basarisiz sonuc hata tasimak zorundadir.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}

public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    internal Result(TValue? value, bool isSuccess, Error error) : base(isSuccess, error) => _value = value;

    /// <summary>Yalnizca IsSuccess dogruyken okunabilir.</summary>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Basarisiz sonucun degeri okunamaz.");
}
