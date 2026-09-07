using FleetOps.SharedKernel.Domain;

namespace FleetOps.Tasks.Domain;

public sealed class TaskAssignment : Entity
{
    internal TaskAssignment(Guid id, Guid taskId, Guid agvId, DateTime assignedAtUtc) : base(id)
    {
        TaskId = taskId;
        AgvId = agvId;
        AssignedAtUtc = assignedAtUtc;
    }

    private TaskAssignment()
    {
    }

    public Guid TaskId { get; private set; }

    public Guid AgvId { get; private set; }

    public DateTime AssignedAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public bool Aktif => CompletedAtUtc is null;

    internal void Kapat(DateTime completedAtUtc) => CompletedAtUtc = completedAtUtc;
}
