// Backend'deki ResourceSummary kaydinin istemci karsiligi.
export interface ResourceSummary {
  id: string;
  code: string;
  kind: string;
  lockedByAgvId: string | null;
  lockExpiresAtUtc: string | null;
}
