import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { LocationSummary, StockBalanceSummary, StockMovementSummary } from './stock.model';
import { PagedResult } from '../sayfalama.model';

@Injectable({ providedIn: 'root' })
export class StockService {
  private readonly http = inject(HttpClient);

  locations(): Observable<LocationSummary[]> {
    return this.http.get<LocationSummary[]>(`${environment.apiUrl}/locations`);
  }

  movements(page = 1, pageSize = 20): Observable<PagedResult<StockMovementSummary>> {
    return this.http.get<PagedResult<StockMovementSummary>>(
      `${environment.apiUrl}/stock/movements`,
      { params: { page, pageSize } },
    );
  }

  balances(page = 1, pageSize = 20, materialCode = ''): Observable<PagedResult<StockBalanceSummary>> {
    const params: Record<string, string | number> = { page, pageSize };

    if (materialCode.trim()) {
      params['materialCode'] = materialCode.trim();
    }

    return this.http.get<PagedResult<StockBalanceSummary>>(
      `${environment.apiUrl}/stock/balances`,
      { params },
    );
  }
}
