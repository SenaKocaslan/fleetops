import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
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

  protected readonly tasks = signal<TaskSummary[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected materialCode = '';
  protected quantity = 1;
  protected priority = 1;

  constructor() {
    this.refresh();
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
