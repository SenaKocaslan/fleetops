namespace FleetOps.Tasks.Application;

// Liste ekraninin ihtiyaci kadar alan. Aggregate donulmez:
// okuma yolu projeksiyon kullanir, is kurallarini bellege almaz.
public sealed record TaskSummary(
    Guid Id,
    string Status,
    string MaterialCode,
    int Quantity,
    int Priority,
    DateTime CreatedAtUtc,
    Guid? AssignedAgvId);
