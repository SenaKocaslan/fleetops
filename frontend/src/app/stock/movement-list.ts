import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { StockService } from './stock.service';
import { StockMovementSummary } from './stock.model';

@Component({
  selector: 'app-movement-list',
  imports: [DatePipe],
  templateUrl: './movement-list.html',
  styleUrl: './movement-list.css',
})
export class MovementList {
  private readonly service = inject(StockService);

  protected readonly movements = signal<StockMovementSummary[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  constructor() {
    this.refresh();
  }

  protected refresh(): void {
    this.loading.set(true);
    this.error.set(null);
    this.service.movements().subscribe({
      next: (kayitlar) => {
        this.movements.set(kayitlar);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Stok hareketleri alinamadi.');
        this.loading.set(false);
      },
    });
  }
}
