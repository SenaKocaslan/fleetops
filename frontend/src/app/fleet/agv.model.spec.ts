import { AgvSummary, agvUygula } from './agv.model';

function agv(kismi: Partial<AgvSummary> & { id: string; code: string }): AgvSummary {
  return {
    status: 'Available',
    batteryLevel: 100,
    gorevAlabilir: true,
    currentLocationId: null,
    lastSeenAtUtc: null,
    ...kismi,
  };
}

describe('agvUygula', () => {
  const a1 = agv({ id: '1', code: 'AGV-01' });
  const a2 = agv({ id: '2', code: 'AGV-02' });

  it('bilinen agvyi gunceller, listeyi buyutmez', () => {
    const sonuc = agvUygula([a1, a2], agv({ id: '1', code: 'AGV-01', batteryLevel: 30 }));

    expect(sonuc.length).toBe(2);
    expect(sonuc.find((a) => a.id === '1')!.batteryLevel).toBe(30);
    expect(sonuc.find((a) => a.id === '2')!.batteryLevel).toBe(100);
  });

  it('bilinmeyen agvyi listeye ekler', () => {
    const sonuc = agvUygula([a1], agv({ id: '9', code: 'AGV-09' }));

    expect(sonuc.map((a) => a.code)).toEqual(['AGV-01', 'AGV-09']);
  });

  it('sirayi koda gore sabit tutar', () => {
    const sonuc = agvUygula([a2], agv({ id: '1', code: 'AGV-01' }));

    expect(sonuc.map((a) => a.code)).toEqual(['AGV-01', 'AGV-02']);
  });

  it('girdi dizisini degistirmez', () => {
    const liste = [a1];

    agvUygula(liste, agv({ id: '1', code: 'AGV-01', batteryLevel: 5 }));

    expect(liste[0].batteryLevel).toBe(100);
  });
});
