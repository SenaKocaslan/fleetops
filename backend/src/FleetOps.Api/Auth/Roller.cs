namespace FleetOps.Api.Auth;

// Rol adlari hem token'da hem policy'lerde geciyor; string tekrari
// yazim hatasina acik oldugu icin tek yerde.
public static class Roller
{
    public const string Operator = "Operator";

    public const string Supervisor = "Supervisor";
}
