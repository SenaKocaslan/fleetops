export interface Alarm {
  code: string;
  severity: 'Kritik' | 'Uyari' | 'Bilgi';
  subject: string;
  message: string;
  detectedAtUtc: string;
}

export interface AlarmGrubu {
  code: string;
  severity: 'Kritik' | 'Uyari' | 'Bilgi';
  count: number;
  shown: number;
}

export interface AlarmYaniti {
  // Her turden en fazla birkac ornek; tamami groups[].count'ta.
  items: Alarm[];
  groups: AlarmGrubu[];
  totalCount: number;
  criticalCount: number;
}

export const BOS_ALARM: AlarmYaniti = { items: [], groups: [], totalCount: 0, criticalCount: 0 };
