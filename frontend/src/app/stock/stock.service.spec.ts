import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { StockService } from './stock.service';
import { environment } from '../../environments/environment';

describe('StockService', () => {
  let service: StockService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(StockService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('lokasyonlari dogru adresten ceker', () => {
    service.locations().subscribe();

    const istek = http.expectOne(`${environment.apiUrl}/locations`);
    expect(istek.request.method).toBe('GET');
    istek.flush([]);
  });

  it('stok hareketlerini dogru adresten ceker', () => {
    service.movements().subscribe();

    const istek = http.expectOne(`${environment.apiUrl}/stock/movements`);
    expect(istek.request.method).toBe('GET');
    istek.flush([]);
  });
});
