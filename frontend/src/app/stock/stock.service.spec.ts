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

    const istek = http.expectOne(
      (i) => i.url === `${environment.apiUrl}/stock/movements`,
    );
    expect(istek.request.method).toBe('GET');
    istek.flush([]);
  });

  it('raf durumunu malzeme filtresiyle ceker, bos filtreyi gondermez', () => {
    service.balances(2, 20, 'MLZ-7').subscribe();
    const filtreli = http.expectOne((i) => i.url === `${environment.apiUrl}/stock/balances`);
    expect(filtreli.request.params.get('materialCode')).toBe('MLZ-7');
    expect(filtreli.request.params.get('page')).toBe('2');
    filtreli.flush({ items: [], page: 2, pageSize: 20, totalCount: 0, totalPages: 0, hasNext: false });

    service.balances().subscribe();
    const filtresiz = http.expectOne((i) => i.url === `${environment.apiUrl}/stock/balances`);
    expect(filtresiz.request.params.has('materialCode')).toBe(false);
    filtresiz.flush({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0, hasNext: false });
  });
});
