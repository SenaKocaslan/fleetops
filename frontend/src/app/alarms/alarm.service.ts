import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { environment } from '../../environments/environment';
import { AlarmYaniti } from './alarm.model';

@Injectable({ providedIn: 'root' })
export class AlarmService {
  private readonly http = inject(HttpClient);

  // Rozet gezinme cubugunda, liste alarm ekraninda: ayni veriyi iki yerde
  // ayri ayri cekmemek icin durum serviste.
  readonly son = signal<AlarmYaniti>({ items: [], criticalCount: 0 });

  yenile(): void {
    this.http.get<AlarmYaniti>(`${environment.apiUrl}/alarms`).subscribe({
      next: (yanit) => this.son.set(yanit),
      error: () => this.son.set({ items: [], criticalCount: 0 }),
    });
  }
}
