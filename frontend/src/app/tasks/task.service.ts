import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { CreateTaskRequest, TaskSummary } from './task.model';

// Bilesenler HTTP detayini bilmez; adres ve sekil burada tek yerde.
@Injectable({ providedIn: 'root' })
export class TaskService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/tasks`;

  list(): Observable<TaskSummary[]> {
    return this.http.get<TaskSummary[]>(this.url);
  }

  create(istek: CreateTaskRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.url, istek);
  }

  assign(taskId: string, agvId: string): Observable<void> {
    return this.http.post<void>(`${this.url}/${taskId}/assign`, { agvId });
  }
}
