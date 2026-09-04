import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { CreateTaskRequest, TaskSummary } from './task.model';
import { PagedResult } from '../sayfalama.model';

@Injectable({ providedIn: 'root' })
export class TaskService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/tasks`;

  list(page = 1, pageSize = 20, materialCode = ''): Observable<PagedResult<TaskSummary>> {
    const params: Record<string, string | number> = { page, pageSize };

    // Bos parametre gondermek "bos koda esit olanlar" gibi okunabilir;
    // hic gondermemek daha net.
    if (materialCode.trim()) {
      params['materialCode'] = materialCode.trim();
    }

    return this.http.get<PagedResult<TaskSummary>>(this.url, { params });
  }

  create(istek: CreateTaskRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.url, istek);
  }

  assign(taskId: string, agvId: string): Observable<void> {
    return this.http.post<void>(`${this.url}/${taskId}/assign`, { agvId });
  }

  start(taskId: string): Observable<void> {
    return this.http.post<void>(`${this.url}/${taskId}/start`, {});
  }

  complete(taskId: string): Observable<void> {
    return this.http.post<void>(`${this.url}/${taskId}/complete`, {});
  }
}
