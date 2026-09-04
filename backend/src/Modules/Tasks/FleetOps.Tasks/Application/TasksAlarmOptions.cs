namespace FleetOps.Tasks.Application;

public sealed class TasksAlarmOptions
{
    public const string Bolum = "TasksAlarms";

    // Bu sureden uzun atanmamis gorev, filonun yetismedigine isaret.
    public TimeSpan BeklemeEsigi { get; set; } = TimeSpan.FromMinutes(10);

    // Reaper temizlemis olmaliydi; hala duruyorsa arka plan servisi calismiyor.
    public TimeSpan KilitGecikmeToleransi { get; set; } = TimeSpan.FromMinutes(1);
}
