import { Oturum, oturumGecerli } from './auth.model';

function oturum(bitis: string): Oturum {
  return { token: 't', userName: 'u', role: 'Operator', expiresAtUtc: bitis };
}

describe('oturumGecerli', () => {
  const simdi = new Date('2026-09-04T12:00:00Z');

  it('null oturum gecersizdir', () => {
    expect(oturumGecerli(null, simdi)).toBe(false);
  });

  it('suresi dolmus oturum gecersizdir', () => {
    expect(oturumGecerli(oturum('2026-09-04T11:59:59Z'), simdi)).toBe(false);
  });

  it('suresi devam eden oturum gecerlidir', () => {
    expect(oturumGecerli(oturum('2026-09-04T12:00:01Z'), simdi)).toBe(true);
  });

  it('bozuk tarih gecersiz sayilir', () => {
    expect(oturumGecerli(oturum('tarih-degil'), simdi)).toBe(false);
  });
});
