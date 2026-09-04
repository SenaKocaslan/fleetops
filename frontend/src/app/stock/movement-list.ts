import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { StockService } from './stock.service';
import { StockMovementSummary } from './stock.model';
import { BOS_SAYFA, PagedResult } from '../sayfalama.model';

@Component({
  selector: 'app-movement-list',
  imports: [DatePipe],
  templateUrl: './movement-list.html',
  styleUrl: './movement-list.css',
})
export class MovementList {
  private readonly service = inject(StockService);

  protected readonly sayfa = signal<PagedResult<StockMovementSummary>>(BOS_SAYFA);
  protected readonly movements = computed(() => this.sayfa().items);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  constructor() {
    this.refresh();
  }

  protected refresh(): void {
    this.loading.set(true);
    this.error.set(null);
    this.service.movements(this.sayfa().page, this.sayfa().pageSize).subscribe({
      next: (kayitlar) => {
        this.sayfa.set(kayitlar);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Stok hareketleri alinamadi.');
        this.loading.set(false);
      },
    });
  }

  protected sayfayaGit(page: number): void {
    if (page < 1 || (page > this.sayfa().totalPages && page !== 1)) {
      return;
    }

    this.sayfa.update((s) => ({ ...s, page }));
    this.refresh();
  }
}
