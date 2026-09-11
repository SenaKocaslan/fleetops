using FleetOps.SharedKernel;

namespace FleetOps.Tasks.Application;

public sealed record StartTaskCommand(Guid TaskId) : ICommand;

public sealed record CompleteTaskCommand(Guid TaskId) : ICommand;

// Atanmis ama baslamamis gorevi havuza geri dondurur. "Tasks.BaslamayanGorev"
// alarminin cozumu bu: alarm calar ama supervizorun basacagi bir dugme yoktu.
public sealed record ReleaseTaskCommand(Guid TaskId) : ICommand;

// Yurutulen gorev tamamlanamadi (arac arizalandi, malzeme bulunamadi...).
public sealed record FailTaskCommand(Guid TaskId) : ICommand;

// Henuz atanmamis gorev geri cekildi. Atanmis gorev once havuza donmeli:
// durum makinesi Assigned -> Cancelled gecisine bilerek izin vermiyor.
public sealed record CancelTaskCommand(Guid TaskId) : ICommand;
