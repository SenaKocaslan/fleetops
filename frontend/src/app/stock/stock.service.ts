import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { LocationSummary, StockMovementSummary } from './stock.model';

@Injectable({ providedIn: 'root' })
export class StockService {
  private readonly http = inject(HttpClient);

  locations(): Observable<LocationSummary[]> {
    return this.http.get<LocationSummary[]>(`${environment.apiUrl}/locations`);
  }

  movements(): Observable<StockMovementSummary[]> {
    return this.http.get<StockMovementSummary[]>(`${environment.apiUrl}/stock/movements`);
  }
}
