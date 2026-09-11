import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AgvService } from '../fleet/agv.service';
import { AgvSummary } from '../fleet/agv.model';
import { StockService } from '../stock/stock.service';
import { LocationSummary } from '../stock/stock.model';
import { TaskService } from './task.service';
import { DispatchService } from './dispatch.service';
import { OtomatikAtamaOzeti } from './dispatch.model';
import { TaskSummary } from './task.model';
import { BOS_SAYFA, PagedResult } from '../sayfalama.model';
import { AuthService } from '../auth/auth.service';

@Component({
  selector: 'app-task-list',
  imports: [FormsModule],
  templateUrl: './task-list.html',
  styleUrl: './task-list.css',
})
export class TaskList {
  private readonly service = inject(TaskService);
  private readonly agvService = inject(AgvService);
  private readonly dispatch = inject(DispatchService);
  private readonly stockService = inject(StockService);

  private readonly auth = inject(AuthService);

  protected readonly sayfa = signal<PagedResult<TaskSummary>>(BOS_SAYFA);
  protected readonly tasks = computed(() => this.sayfa().items);

  // Gorev acma ve atama Supervisor isi; yetkisiz kullaniciya calismayacak
  // kontrolleri gostermek yaniltici olur. Sunucu zaten 403 doner, bu sadece
  // arayuz nezaketi.
  protected readonly planlamaYetkisi = this.auth.supervisorMu;
  protected readonly agvs = signal<AgvSummary[]>([]);
  protected readonly locations = signal<LocationSummary[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly assignError = signal<string | null>(null);

  protected readonly secim = signal<Record<string, string>>({});

  protected readonly dagitimSonucu = signal<OtomatikAtamaOzeti | null>(null);
  protected readonly dagitimCalisiyor = signal(false);

  protected readonly agvKodlari = computed(() =>
    Object.fromEntries(this.agvs().map((a) => [a.id, a.code])),
  );

  protected readonly musaitAgvler = computed(() => this.agvs().filter((a) => a.gorevAlabilir));

  protected materialCode = '';
  protected readonly arama = signal('');

  // Bos = tum durumlar. Bitmis gorevler zaten listenin altina duser;
  // bu secici "yalnizca sunlari goster" demek isteyenler icin.
  protected readonly durumSecimi = signal('');
  protected readonly durumlar = [
    { deger: '', etiket: 'Tum durumlar' },
    { deger: 'Pending', etiket: 'Bekleyen' },
    { deger: 'Assigned', etiket: 'Atanmis' },
    { deger: 'InProgress', etiket: 'Yurutuluyor' },
    { deger: 'Completed', etiket: 'Tamamlanan' },
  ];
  protected quantity = 1;
  protected priority = 1;
  protected fromLocationId = '';
  protected toLocationId = '';

  constructor() {
    this.refresh();
    this.agvService.list().subscribe({
      next: (kayitlar) => this.agvs.set(kayitlar),
      error: () => this.agvs.set([]),
    });

    this.stockService.locations().subscribe({
      next: (kayitlar) => {
        this.locations.set(kayitlar);
        this.fromLocationId = kayitlar[0]?.id ?? '';
        this.toLocationId = kayitlar[1]?.id ?? '';
      },
      error: () => this.locations.set([]),
    });
  }

  protected start(taskId: string): void {
    this.durumDegistir(this.service.start(taskId));
  }

  protected complete(taskId: string): void {
    this.durumDegistir(this.service.complete(taskId));
  }

  protected release(taskId: string): void {
    this.durumDegistir(this.service.release(taskId));
  }

  // Geri alinamayan iki islem icin onay: yanlis tiklama gorevi bitirir.
  protected fail(taskId: string): void {
    if (confirm('Gorev basarisiz olarak kapatilacak. Emin misiniz?')) {
      this.durumDegistir(this.service.fail(taskId));
    }
  }

  protected cancel(taskId: string): void {
    if (confirm('Gorev iptal edilecek. Emin misiniz?')) {
      this.durumDegistir(this.service.cancel(taskId));
    }
  }

  private durumDegistir(istek: import('rxjs').Observable<void>): void {
    this.assignError.set(null);
    istek.subscribe({
      next: () => this.refresh(),
      error: (yanit) => {
        this.assignError.set(yanit?.error?.message ?? 'Gorev durumu degistirilemedi.');
        this.refresh();
      },
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
        this.assignError.set(
          yanit?.status === 409
            ? 'Gorev bu sirada baska bir istek tarafindan atandi. Listeyi yenileyip tekrar deneyin.'
            : (yanit?.error?.message ?? 'Atama yapilamadi.'),
        );
        this.refresh();
      },
    });
  }

  // Havuzdaki bekleyen gorevleri musait araclara dagitir. Hangi aracin
  // hangi gorevi alacagina sunucudaki strateji karar veriyor; arayuz
  // yalnizca tetikliyor ve sonucu gosteriyor.
  protected otomatikAta(): void {
    this.assignError.set(null);
    this.dagitimSonucu.set(null);
    this.dagitimCalisiyor.set(true);

    this.dispatch.otomatikAta().subscribe({
      next: (ozet) => {
        this.dagitimSonucu.set(ozet);
        this.dagitimCalisiyor.set(false);

        // Atanan gorevler oncelige gore havuzun basindaydi; arama filtresi
        // aciksa kullanici sonucu goremez.
        this.arama.set('');
        this.sayfayaGit(1);

        // AGV listesi bilesenin kurulusunda cekiliyordu; dagitimdan sonra
        // musait arac kumesi degisti.
        this.agvService.list().subscribe({
          next: (kayitlar) => this.agvs.set(kayitlar),
          error: () => undefined,
        });
      },
      error: (yanit) => {
        this.dagitimCalisiyor.set(false);
        this.assignError.set(yanit?.error?.message ?? 'Otomatik atama yapilamadi.');
      },
    });
  }

  protected dagitimMetni(ozet: OtomatikAtamaOzeti): string {
    return ozet.atananlar.map((a) => `${a.materialCode} -> ${a.agvCode}`).join(', ');
  }

  protected refresh(): void {
    this.loading.set(true);
    this.error.set(null);
    this.service
      .list(this.sayfa().page, this.sayfa().pageSize, this.arama(), this.durumSecimi())
      .subscribe({
        next: (kayitlar) => {
          this.sayfa.set(kayitlar);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Gorev listesi alinamadi. API calisiyor mu?');
          this.loading.set(false);
        },
      });
  }

  protected durumDegisti(olay: Event): void {
    this.durumSecimi.set((olay.target as HTMLSelectElement).value);
    // Filtre daralinca 3. sayfada kalmak bos liste gosterirdi.
    this.sayfa.update((s) => ({ ...s, page: 1 }));
    this.refresh();
  }

  protected aramaDegisti(olay: Event): void {
    this.arama.set((olay.target as HTMLInputElement).value);
    // Filtre degisince 3. sayfada kalmak bos liste gosterirdi.
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

  protected create(): void {
    this.error.set(null);

    this.service
      .create({
        fromLocationId: this.fromLocationId,
        toLocationId: this.toLocationId,
        materialCode: this.materialCode,
        quantity: this.quantity,
        priority: this.priority,
      })
      .subscribe({
        next: () => {
          // Yeni kayit oncelige gore siralanmis havuzda ilk sayfada
          // olmayabilir; aramayi kendi koduna ayarlayip gosteriyoruz.
          this.arama.set(this.materialCode);
          this.materialCode = '';
          this.quantity = 1;
          this.priority = 1;
          // Yeni kayit ilk sayfada; kullanici 3. sayfadaysa olusturdugu
          // gorevi goremezdi.
          this.sayfayaGit(1);
        },
        error: (yanit) => {
          this.error.set(yanit?.error?.message ?? 'Gorev olusturulamadi.');
        },
      });
  }
}
