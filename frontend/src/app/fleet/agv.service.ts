import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { AgvSummary } from './agv.model';

// Fleet modulu backend'de ayri; istemcide de ayri servis. Tasks servisine
// koysaydik modul siniri yalnizca sunucuda kalirdi.
@Injectable({ providedIn: 'root' })
export class AgvService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/agvs`;

  list(): Observable<AgvSummary[]> {
    return this.http.get<AgvSummary[]>(this.url);
  }
}
