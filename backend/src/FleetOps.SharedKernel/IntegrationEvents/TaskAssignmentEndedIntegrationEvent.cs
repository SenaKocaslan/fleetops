namespace FleetOps.SharedKernel.IntegrationEvents;

// Sebep metin: Tasks'in domain sabitlerini SharedKernel'e tasimamak icin.
// Tuketici (Fleet) sebebe gore davranmiyor; yalnizca kayit ve teshis icin.
public sealed record TaskAssignmentEndedIntegrationEvent(
    Guid Id,
    DateTime OccurredAtUtc,
    Guid TaskId,
    Guid AgvId,
    string Sebep) : IntegrationEvent(Id, OccurredAtUtc);
