import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AgvService } from './agv.service';
import { AgvSummary } from './agv.model';
import { environment } from '../../environments/environment';

describe('AgvService', () => {
  let service: AgvService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AgvService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('agv listesini dogru adresten ceker', () => {
    const beklenen: AgvSummary[] = [
      { id: 'a1', code: 'AGV-01', status: 'Available', batteryLevel: 95, gorevAlabilir: true },
    ];

    let gelen: AgvSummary[] | undefined;
    service.list().subscribe((v) => (gelen = v));

    const istek = http.expectOne(`${environment.apiUrl}/agvs`);
    expect(istek.request.method).toBe('GET');
    istek.flush(beklenen);

    expect(gelen).toEqual(beklenen);
  });
});
