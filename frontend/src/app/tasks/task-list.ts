import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AgvService } from '../fleet/agv.service';
import { AgvSummary } from '../fleet/agv.model';
import { TaskService } from './task.service';
import { TaskSummary } from './task.model';

@Component({
  selector: 'app-task-list',
  imports: [FormsModule],
  templateUrl: './task-list.html',
  styleUrl: './task-list.css',
})
export class TaskList {
  private readonly service = inject(TaskService);
  private readonly agvService = inject(AgvService);

  protected readonly tasks = signal<TaskSummary[]>([]);
  protected readonly agvs = signal<AgvSummary[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly assignError = signal<string | null>(null);

  // Gorev basina secilen AGV. Anahtar gorev kimligi.
  protected readonly secim = signal<Record<string, string>>({});

  // Listede ham Guid yerine AGV kodu gosterilir.
  protected readonly agvKodlari = computed(() =>
    Object.fromEntries(this.agvs().map((a) => [a.id, a.code])),
  );

  // Yalnizca gorev alabilecek AGV'ler secilebilir. Kural sunucuda
  // hesaplanip geliyor; burada tekrar yazilmiyor.
  protected readonly musaitAgvler = computed(() => this.agvs().filter((a) => a.gorevAlabilir));

  protected materialCode = '';
  protected quantity = 1;
  protected priority = 1;

  constructor() {
    this.refresh();
    this.agvService.list().subscribe({
      next: (kayitlar) => this.agvs.set(kayitlar),
      // AGV listesi alinamazsa gorev listesi yine calisir; atama yapilamaz.
      error: () => this.agvs.set([]),
    });
  }

  protected agvKodu(id: string | null): string {
    return id ? (this.agvKodlari()[id] ?? id) : '-';
  }

  protected secimYap(taskId: string, olay: Event): void {
    const agvId = (olay.target as HTMLSelectElement).value;
    this.secim.update((mevcut) => ({ ...mevcut, [taskId]: agvId }));
  }

  protected assign(taskId: string): void {
    const agvId = this.secim()[taskId];
    if (!agvId) {
      return;
    }

    this.assignError.set(null);
    this.service.assign(taskId, agvId).subscribe({
      next: () => this.refresh(),
      error: (yanit) => {
        // 409: yarisi kaybettik. Kullaniciya "hata" degil "tekrar dene"
        // demek dogru olan; istek yanlis degildi.
        this.assignError.set(
          yanit?.status === 409
            ? 'Gorev bu sirada baska bir istek tarafindan atandi. Listeyi yenileyip tekrar deneyin.'
            : (yanit?.error?.message ?? 'Atama yapilamadi.'),
        );
        this.refresh();
      },
    });
  }

  protected refresh(): void {
    this.loading.set(true);
    this.error.set(null);
    this.service.list().subscribe({
      next: (kayitlar) => {
        this.tasks.set(kayitlar);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Gorev listesi alinamadi. API calisiyor mu?');
        this.loading.set(false);
      },
    });
  }

  protected create(): void {
    this.error.set(null);

    // Lokasyonlar Stock modulunde tanimlanacak (Gun 6). O zamana kadar
    // gecici olarak uretiliyor; form da bunu ekranda belirtiyor.
    this.service
      .create({
        fromLocationId: crypto.randomUUID(),
        toLocationId: crypto.randomUUID(),
        materialCode: this.materialCode,
        quantity: this.quantity,
        priority: this.priority,
      })
      .subscribe({
        next: () => {
          this.materialCode = '';
          this.quantity = 1;
          this.priority = 1;
          this.refresh();
        },
        error: (yanit) => {
          this.error.set(yanit?.error?.message ?? 'Gorev olusturulamadi.');
        },
      });
  }
}
