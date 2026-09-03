import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TaskService } from './task.service';
import { TaskSummary } from './task.model';
import { environment } from '../../environments/environment';

describe('TaskService', () => {
  let service: TaskService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(TaskService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('gorev listesini dogru adresten ceker', () => {
    const beklenen: TaskSummary[] = [
      {
        id: 'a1',
        status: 'Pending',
        materialCode: 'MLZ-100',
        quantity: 5,
        priority: 1,
        createdAtUtc: '2026-09-02T10:00:00Z',
        assignedAgvId: null,
      },
    ];

    let gelen: TaskSummary[] | undefined;
    service.list().subscribe((v) => (gelen = v));

    const istek = http.expectOne(`${environment.apiUrl}/tasks`);
    expect(istek.request.method).toBe('GET');
    istek.flush(beklenen);

    expect(gelen).toEqual(beklenen);
  });

  it('gorev olusturmayi POST ile gonderir', () => {
    const govde = {
      fromLocationId: 'f1',
      toLocationId: 't1',
      materialCode: 'MLZ-200',
      quantity: 3,
      priority: 2,
    };

    service.create(govde).subscribe();

    const istek = http.expectOne(`${environment.apiUrl}/tasks`);
    expect(istek.request.method).toBe('POST');
    expect(istek.request.body).toEqual(govde);
    istek.flush({ id: 'yeni' });
  });

  it('atamayi gorevin alt adresine POST eder', () => {
    service.assign('gorev-1', 'agv-1').subscribe();

    const istek = http.expectOne(`${environment.apiUrl}/tasks/gorev-1/assign`);
    expect(istek.request.method).toBe('POST');
    expect(istek.request.body).toEqual({ agvId: 'agv-1' });
    istek.flush(null);
  });

  it('durum gecislerini kendi alt adreslerine POST eder', () => {
    service.start('gorev-1').subscribe();
    const baslat = http.expectOne(`${environment.apiUrl}/tasks/gorev-1/start`);
    expect(baslat.request.method).toBe('POST');
    baslat.flush(null);

    service.complete('gorev-1').subscribe();
    const tamamla = http.expectOne(`${environment.apiUrl}/tasks/gorev-1/complete`);
    expect(tamamla.request.method).toBe('POST');
    tamamla.flush(null);
  });
});
