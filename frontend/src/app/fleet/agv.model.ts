// Backend'deki AgvSummary kaydinin istemci karsiligi.
export interface AgvSummary {
  id: string;
  code: string;
  status: string;
  batteryLevel: number;
  gorevAlabilir: boolean;
}
