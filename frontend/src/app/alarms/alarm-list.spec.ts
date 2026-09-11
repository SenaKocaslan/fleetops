import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { AlarmList } from './alarm-list';
import { AlarmService } from './alarm.service';
import { Alarm, AlarmYaniti } from './alarm.model';

function alarm(code: string, severity: Alarm['severity'], subject: string): Alarm {
  return { code, severity, subject, message: 'm', detectedAtUtc: '2026-09-11T10:00:00Z' };
}

describe('AlarmList', () => {
  function ciz(yanit: AlarmYaniti): HTMLElement {
    TestBed.configureTestingModule({
      imports: [AlarmList],
      providers: [{ provide: AlarmService, useValue: { son: signal(yanit), yenile: () => undefined } }],
    });
    const fixture = TestBed.createComponent(AlarmList);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('alarmlari ture gore gruplar ve gosterilmeyenleri sayi olarak yazar', () => {
    const el = ciz({
      items: [
        alarm('Tasks.TeslimEdilemeyenOlay', 'Kritik', 'm1'),
        alarm('Tasks.UzunSureBekleyenGorev', 'Uyari', 'g1'),
        alarm('Tasks.UzunSureBekleyenGorev', 'Uyari', 'g2'),
      ],
      groups: [
        { code: 'Tasks.TeslimEdilemeyenOlay', severity: 'Kritik', count: 1, shown: 1 },
        { code: 'Tasks.UzunSureBekleyenGorev', severity: 'Uyari', count: 51, shown: 2 },
      ],
      totalCount: 52,
      criticalCount: 1,
    });

    const gruplar = el.querySelectorAll('[data-testid="alarm-grup"]');
    expect(gruplar.length).toBe(2);
    expect(gruplar[1].textContent).toContain('51 alarm');

    expect(el.querySelectorAll('[data-testid="alarm-row"]').length).toBe(3);

    // Yalnizca kesilen grupta "devami" satiri olmali.
    const devami = el.querySelectorAll('[data-testid="alarm-devami"]');
    expect(devami.length).toBe(1);
    expect(devami[0].textContent).toContain('+49 benzer alarm daha');
  });
});
