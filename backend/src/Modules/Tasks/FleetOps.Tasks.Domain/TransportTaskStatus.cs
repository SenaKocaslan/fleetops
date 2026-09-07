namespace FleetOps.Tasks.Domain;

public enum TransportTaskStatus
{
    Pending = 1,

    Assigned = 2,

    InProgress = 3,

    Completed = 4,

    Failed = 5,

    Cancelled = 6,
}
