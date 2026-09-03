import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ResourceSummary } from './resource.model';

@Injectable({ providedIn: 'root' })
export class ResourceService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/resources`;

  list(): Observable<ResourceSummary[]> {
    return this.http.get<ResourceSummary[]>(this.url);
  }

  lock(resourceId: string, agvId: string): Observable<{ lockId: string }> {
    return this.http.post<{ lockId: string }>(`${this.url}/${resourceId}/lock`, { agvId });
  }

  release(resourceId: string, agvId: string): Observable<void> {
    return this.http.post<void>(`${this.url}/${resourceId}/release`, { agvId });
  }
}
