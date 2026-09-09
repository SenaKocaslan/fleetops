namespace FleetOps.Tasks.Application;

public sealed class TasksAlarmOptions
{
    public const string Bolum = "TasksAlarms";

    // Bu sureden uzun atanmamis gorev, filonun yetismedigine isaret.
    public TimeSpan BeklemeEsigi { get; set; } = TimeSpan.FromMinutes(10);

    // Atandi ama baslamadi. Sebebi ne olursa olsun (arac atama olayini hic
    // alamadi, sarja girdi, sahada arizalandi) sonuc ayni: gorev kimsenin
    // beklemedigi bir yerde asili kaldi. Bu alarm olmadan gorev sonsuza
    // kadar Assigned kalir ve hicbir yerde iz birakmaz.
    public TimeSpan BaslamaEsigi { get; set; } = TimeSpan.FromMinutes(5);

    // Reaper temizlemis olmaliydi; hala duruyorsa arka plan servisi calismiyor.
    public TimeSpan KilitGecikmeToleransi { get; set; } = TimeSpan.FromMinutes(1);
}
