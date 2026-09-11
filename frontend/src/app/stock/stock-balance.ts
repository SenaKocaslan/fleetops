import { Component, computed, inject, signal } from '@angular/core';
import { StockService } from './stock.service';
import { StockBalanceSummary } from './stock.model';
import { BOS_SAYFA, PagedResult } from '../sayfalama.model';

@Component({
  selector: 'app-stock-balance',
  templateUrl: './stock-balance.html',
  styleUrl: './movement-list.css',
})
export class StockBalance {
  private readonly service = inject(StockService);

  protected readonly sayfa = signal<PagedResult<StockBalanceSummary>>(BOS_SAYFA);
  protected readonly bakiyeler = computed(() => this.sayfa().items);
  protected readonly arama = signal('');
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  constructor() {
    this.refresh();
  }

  protected refresh(): void {
    this.loading.set(true);
    this.error.set(null);
    this.service.balances(this.sayfa().page, this.sayfa().pageSize, this.arama()).subscribe({
      next: (kayitlar) => {
        this.sayfa.set(kayitlar);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Stok bakiyesi alinamadi.');
        this.loading.set(false);
      },
    });
  }

  protected aramaDegisti(olay: Event): void {
    this.arama.set((olay.target as HTMLInputElement).value);
    this.sayfa.update((s) => ({ ...s, page: 1 }));
    this.refresh();
  }

  protected sayfayaGit(page: number): void {
    if (page < 1 || (page > this.sayfa().totalPages && page !== 1)) {
      return;
    }

    this.sayfa.update((s) => ({ ...s, page }));
    this.refresh();
  }
}
