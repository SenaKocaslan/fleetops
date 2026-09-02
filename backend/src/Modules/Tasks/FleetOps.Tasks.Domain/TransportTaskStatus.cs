namespace FleetOps.Tasks.Domain;

public enum TransportTaskStatus
{
    // Havuzda, henuz bir AGV'ye atanmadi.
    Pending = 1,

    // Bir AGV'ye atandi, henuz baslamadi.
    Assigned = 2,

    // AGV gorevi yurutuyor.
    InProgress = 3,

    // Basariyla tamamlandi. Stok hareketi bu noktada tetiklenir.
    Completed = 4,

    // Yurutme sirasinda basarisiz oldu.
    Failed = 5,

    // Baslamadan iptal edildi.
    Cancelled = 6,
}
