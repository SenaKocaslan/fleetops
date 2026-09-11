import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TaskService } from './task.service';
import { TaskSummary } from './task.model';
import { environment } from '../../environments/environment';
import { PagedResult } from '../sayfalama.model';

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

    const sayfa: PagedResult<TaskSummary> = {
      items: beklenen,
      page: 2,
      pageSize: 10,
      totalCount: 25,
      totalPages: 3,
      hasNext: true,
    };

    let gelen: PagedResult<TaskSummary> | undefined;
    service.list(2, 10).subscribe((v) => (gelen = v));

    const istek = http.expectOne(
      (i) => i.url === `${environment.apiUrl}/tasks` && i.params.get('page') === '2',
    );
    expect(istek.request.method).toBe('GET');
    expect(istek.request.params.get('pageSize')).toBe('10');
    istek.flush(sayfa);

    expect(gelen).toEqual(sayfa);
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

  it('durum filtresini sorgu parametresi olarak gonderir', () => {
    service.list(1, 20, '', 'Pending').subscribe();

    const istek = http.expectOne(
      (i) => i.url === `${environment.apiUrl}/tasks` && i.params.get('status') === 'Pending',
    );

    istek.flush({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0, hasNext: false });
  });

  it('durum bos ise status parametresi hic gonderilmez', () => {
    // Bos parametre "durumu bos olanlar" gibi okunabilir; hic gondermemek net.
    service.list().subscribe();

    const istek = http.expectOne((i) => i.url === `${environment.apiUrl}/tasks`);
    expect(istek.request.params.has('status')).toBe(false);

    istek.flush({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0, hasNext: false });
  });

  it('havuza dondurme, basarisiz ve iptal dogru adreslere POST eder', () => {
    service.release('g1').subscribe();
    service.fail('g2').subscribe();
    service.cancel('g3').subscribe();

    for (const [id, islem] of [['g1', 'release'], ['g2', 'fail'], ['g3', 'cancel']]) {
      const istek = http.expectOne(`${environment.apiUrl}/tasks/${id}/${islem}`);
      expect(istek.request.method).toBe('POST');
      istek.flush(null);
    }
  });
});
