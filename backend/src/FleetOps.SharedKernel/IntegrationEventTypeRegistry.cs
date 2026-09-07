using System.Reflection;

namespace FleetOps.SharedKernel;

// Outbox satirinda tur KISA adla saklanir. AssemblyQualifiedName assembly
// adi ve surumu icerir; tur tasininca tabloda duran satirlar okunamaz olur.
public interface IIntegrationEventTypeRegistry
{
    Type? Cozumle(string ad);
}

public sealed class IntegrationEventTypeRegistry : IIntegrationEventTypeRegistry
{
    private readonly Dictionary<string, Type> _turler;

    public IntegrationEventTypeRegistry()
    {
        _turler = typeof(IntegrationEvent).Assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false } && t.IsAssignableTo(typeof(IntegrationEvent)))
            .ToDictionary(t => t.Name, StringComparer.Ordinal);
    }

    public static string Ad(IntegrationEvent olay) => olay.GetType().Name;

    public Type? Cozumle(string ad) => _turler.GetValueOrDefault(ad);
}
