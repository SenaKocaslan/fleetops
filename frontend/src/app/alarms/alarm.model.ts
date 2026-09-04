export interface Alarm {
  code: string;
  severity: 'Kritik' | 'Uyari' | 'Bilgi';
  subject: string;
  message: string;
  detectedAtUtc: string;
}

export interface AlarmYaniti {
  items: Alarm[];
  criticalCount: number;
}
