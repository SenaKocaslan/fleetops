using System.Security.Cryptography;
using System.Text;

namespace FleetOps.Api.Auth;

// PBKDF2 (Rfc2898) BCL'de var; ayri bir NuGet paketi gerekmiyor.
// ASP.NET Core Identity'nin kendi hashleyicisi de ayni algoritmayi kullaniyor.
// Parola ASLA duz metin saklanmaz; tuz her kullanici icin farkli uretilir ki
// ayni parolayi kullanan iki kullanici ayni hash'e sahip olmasin.
public static class ParolaHashleyici
{
    private const int TuzUzunlugu = 16;
    private const int HashUzunlugu = 32;
    private const int Tekrar = 100_000;

    public static string Hashle(string parola)
    {
        var tuz = RandomNumberGenerator.GetBytes(TuzUzunlugu);
        var hash = Turet(parola, tuz);

        return $"{Tekrar}.{Convert.ToBase64String(tuz)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Dogrula(string parola, string saklanan)
    {
        var parcalar = saklanan.Split('.');
        if (parcalar.Length != 3 || !int.TryParse(parcalar[0], out var tekrar))
        {
            return false;
        }

        byte[] tuz;
        byte[] beklenen;
        try
        {
            tuz = Convert.FromBase64String(parcalar[1]);
            beklenen = Convert.FromBase64String(parcalar[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        var hesaplanan = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(parola), tuz, tekrar, HashAlgorithmName.SHA256, beklenen.Length);

        // Sabit zamanli karsilastirma: normal esitlik ilk farkli byte'ta
        // donerdi ve gecen sure hash hakkinda bilgi sizdirirdi.
        return CryptographicOperations.FixedTimeEquals(hesaplanan, beklenen);
    }

    private static byte[] Turet(string parola, byte[] tuz) => Rfc2898DeriveBytes.Pbkdf2(
        Encoding.UTF8.GetBytes(parola), tuz, Tekrar, HashAlgorithmName.SHA256, HashUzunlugu);
}
