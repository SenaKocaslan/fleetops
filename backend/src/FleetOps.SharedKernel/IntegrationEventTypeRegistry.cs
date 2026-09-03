using System.Reflection;

namespace FleetOps.SharedKernel;

// Outbox satirinda olayin turu KISA adiyla saklanir.
// AssemblyQualifiedName yazsaydik metin assembly adini ve surumunu de
// icerirdi; turu baska bir namespace'e tasidigimiz veya surum
// yukselttigimiz anda tabloda duran eski satirlar okunamaz olurdu.
public interface IIntegrationEventTypeRegistry
{
    Type? Cozumle(string ad);
}

public sealed class IntegrationEventTypeRegistry : IIntegrationEventTypeRegistry
{
    private readonly Dictionary<string, Type> _turler;

    public IntegrationEventTypeRegistry()
    {
        // Sozlesmelerin tamami SharedKernel'de oldugu icin tek assembly
        // taraniyor. Modul assembly'lerini taramak gerekmiyor - zaten
        // moduller birbirinin turunu goremez.
        _turler = typeof(IntegrationEvent).Assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false } && t.IsAssignableTo(typeof(IntegrationEvent)))
            .ToDictionary(t => t.Name, StringComparer.Ordinal);
    }

    // Yazma tarafi (outbox satirini olusturan DbContext) durum tutmaz,
    // bu yuzden static. Okuma tarafi tur tablosuna ihtiyac duyar.
    public static string Ad(IntegrationEvent olay) => olay.GetType().Name;

    public Type? Cozumle(string ad) => _turler.GetValueOrDefault(ad);
}
