import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ResourceService } from './resource.service';
import { ResourceSummary } from './resource.model';
import { environment } from '../../environments/environment';

describe('ResourceService', () => {
  let service: ResourceService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ResourceService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('kaynak listesini dogru adresten ceker', () => {
    const beklenen: ResourceSummary[] = [
      {
        id: 'r1',
        code: 'DOCK-1',
        kind: 'ChargingDock',
        lockedByAgvId: null,
        lockExpiresAtUtc: null,
      },
    ];

    let gelen: ResourceSummary[] | undefined;
    service.list().subscribe((v) => (gelen = v));

    const istek = http.expectOne(`${environment.apiUrl}/resources`);
    expect(istek.request.method).toBe('GET');
    istek.flush(beklenen);

    expect(gelen).toEqual(beklenen);
  });

  it('kilitlemeyi kaynagin alt adresine POST eder', () => {
    service.lock('r1', 'agv-1').subscribe();

    const istek = http.expectOne(`${environment.apiUrl}/resources/r1/lock`);
    expect(istek.request.method).toBe('POST');
    expect(istek.request.body).toEqual({ agvId: 'agv-1' });
    istek.flush({ lockId: 'k1' });
  });

  it('birakmayi kaynagin alt adresine POST eder', () => {
    service.release('r1', 'agv-1').subscribe();

    const istek = http.expectOne(`${environment.apiUrl}/resources/r1/release`);
    expect(istek.request.method).toBe('POST');
    expect(istek.request.body).toEqual({ agvId: 'agv-1' });
    istek.flush(null);
  });
});
