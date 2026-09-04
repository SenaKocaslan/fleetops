export interface LoginYaniti {
  token: string;
  expiresAtUtc: string;
  userName: string;
  role: string;
}

export interface Oturum {
  token: string;
  userName: string;
  role: string;
  expiresAtUtc: string;
}

export const ROL_SUPERVISOR = 'Supervisor';
export const ROL_OPERATOR = 'Operator';

// Token'in suresi dolmussa depodaki oturum yok sayilir; aksi halde her istek
// 401 alir ve kullanici sebebini goremez.
export function oturumGecerli(oturum: Oturum | null, simdi = new Date()): boolean {
  if (!oturum) {
    return false;
  }

  const bitis = new Date(oturum.expiresAtUtc);
  return Number.isFinite(bitis.getTime()) && bitis > simdi;
}
