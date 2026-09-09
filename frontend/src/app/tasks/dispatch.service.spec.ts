import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { DispatchService } from './dispatch.service';
import { OtomatikAtamaOzeti } from './dispatch.model';
import { environment } from '../../environments/environment';

describe('DispatchService', () => {
  let service: DispatchService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(DispatchService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('otomatik atamayi dogru adrese POST eder', () => {
    const beklenen: OtomatikAtamaOzeti = {
      atananlar: [
        { taskId: 't1', materialCode: 'MLZ-1', priority: 5, agvId: 'a1', agvCode: 'AGV-02' },
      ],
      bekleyenGorev: 3,
      musaitAgv: 1,
      strateji: 'EnYuksekBatarya',
    };

    let sonuc: OtomatikAtamaOzeti | undefined;
    service.otomatikAta().subscribe((o) => (sonuc = o));

    const istek = http.expectOne(`${environment.apiUrl}/dispatch/auto-assign`);
    expect(istek.request.method).toBe('POST');
    istek.flush(beklenen);

    expect(sonuc?.atananlar[0].agvCode).toBe('AGV-02');
    expect(sonuc?.strateji).toBe('EnYuksekBatarya');
  });
});
