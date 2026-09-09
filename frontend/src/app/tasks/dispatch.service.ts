import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { OtomatikAtamaOzeti } from './dispatch.model';

@Injectable({ providedIn: 'root' })
export class DispatchService {
  private readonly http = inject(HttpClient);

  otomatikAta(): Observable<OtomatikAtamaOzeti> {
    return this.http.post<OtomatikAtamaOzeti>(
      `${environment.apiUrl}/dispatch/auto-assign`,
      {},
    );
  }
}
