import { Component, computed, inject, signal } from '@angular/core';
import { AgvService } from '../fleet/agv.service';
import { AgvSummary } from '../fleet/agv.model';
import { ResourceService } from './resource.service';
import { ResourceSummary } from './resource.model';

@Component({
  selector: 'app-resource-list',
  imports: [],
  templateUrl: './resource-list.html',
  styleUrl: './resource-list.css',
})
export class ResourceList {
  private readonly service = inject(ResourceService);
  private readonly agvService = inject(AgvService);

  protected readonly resources = signal<ResourceSummary[]>([]);
  protected readonly agvs = signal<AgvSummary[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly lockError = signal<string | null>(null);

  // Kaynak basina secilen AGV. Anahtar kaynak kimligi.
  protected readonly secim = signal<Record<string, string>>({});

  protected readonly agvKodlari = computed(() =>
    Object.fromEntries(this.agvs().map((a) => [a.id, a.code])),
  );

  constructor() {
    this.refresh();
    this.agvService.list().subscribe({
      next: (kayitlar) => this.agvs.set(kayitlar),
      error: () => this.agvs.set([]),
    });
  }

  protected agvKodu(id: string | null): string {
    return id ? (this.agvKodlari()[id] ?? id) : '-';
  }

  protected refresh(): void {
    this.loading.set(true);
    this.error.set(null);
    this.service.list().subscribe({
      next: (kayitlar) => {
        this.resources.set(kayitlar);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Kaynak listesi alinamadi.');
        this.loading.set(false);
      },
    });
  }

  protected secimYap(resourceId: string, olay: Event): void {
    const agvId = (olay.target as HTMLSelectElement).value;
    this.secim.update((mevcut) => ({ ...mevcut, [resourceId]: agvId }));
  }

  protected lock(resourceId: string): void {
    const agvId = this.secim()[resourceId];
    if (!agvId) {
      return;
    }

    this.lockError.set(null);
    this.service.lock(resourceId, agvId).subscribe({
      next: () => this.refresh(),
      error: (yanit) => this.hatayiGoster(yanit, 'Kaynak kilitlenemedi.'),
    });
  }

  protected release(resource: ResourceSummary): void {
    if (!resource.lockedByAgvId) {
      return;
    }

    this.lockError.set(null);
    // Kilidi yalnizca tutan AGV birakabilir; tutani listeden biliyoruz.
    this.service.release(resource.id, resource.lockedByAgvId).subscribe({
      next: () => this.refresh(),
      error: (yanit) => this.hatayiGoster(yanit, 'Kilit birakilamadi.'),
    });
  }

  private hatayiGoster(yanit: { status?: number; error?: { message?: string } }, varsayilan: string): void {
    // 409: istek yanlis degildi, kaynak o anda baskasindaydi.
    this.lockError.set(
      yanit?.status === 409
        ? (yanit?.error?.message ?? 'Kaynak su anda baskasi tarafindan kilitli.')
        : (yanit?.error?.message ?? varsayilan),
    );
    this.refresh();
  }
}
