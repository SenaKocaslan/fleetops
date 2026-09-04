export interface AgvSummary {
  id: string;
  code: string;
  status: string;
  batteryLevel: number;
  gorevAlabilir: boolean;
  currentLocationId: string | null;
  lastSeenAtUtc: string | null;
}

// Gelen telemetri mevcut listeye uygulanir. Bilinmeyen bir AGV gelirse listeye
// eklenir; sira kod'a gore sabit tutulur ki satirlar canli akista ziplamasin.
export function agvUygula(mevcut: AgvSummary[], gelen: AgvSummary): AgvSummary[] {
  const bulundu = mevcut.some((a) => a.id === gelen.id);
  const yeni = bulundu ? mevcut.map((a) => (a.id === gelen.id ? gelen : a)) : [...mevcut, gelen];
  return yeni.sort((a, b) => a.code.localeCompare(b.code));
}
