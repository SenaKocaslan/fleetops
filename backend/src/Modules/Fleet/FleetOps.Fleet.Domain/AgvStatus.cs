namespace FleetOps.Fleet.Domain;

public enum AgvStatus
{
    // Gorev alabilir.
    Available = 1,

    // Bir goreve atanmis veya gorevi yurutuyor.
    Busy = 2,

    // Sarj oluyor, gorev alamaz.
    Charging = 3,

    // Ariza/bakim. Gorev alamaz.
    OutOfService = 4,
}
