import { Injectable, inject, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { environment } from '../../environments/environment';
import { AgvService } from './agv.service';
import { AgvSummary, agvUygula } from './agv.model';

@Injectable({ providedIn: 'root' })
export class FleetLiveService {
  private readonly agvService = inject(AgvService);
  private connection: HubConnection | null = null;

  readonly agvs = signal<AgvSummary[]>([]);
  readonly connected = signal(false);
  readonly error = signal<string | null>(null);

  async start(): Promise<void> {
    // Once HTTP ile mevcut durum alinir: hub yalnizca DEGISIKLIK yayinlar,
    // acilista tam liste gonderen bir mekanizmasi yok.
    this.agvService.list().subscribe({
      next: (liste) => this.agvs.set(liste),
      error: () => this.error.set('Filo listesi alinamadi.'),
    });

    if (this.connection) {
      return;
    }

    const baglanti = new HubConnectionBuilder()
      .withUrl(environment.hubUrl)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    baglanti.on('agvDegisti', (agv: AgvSummary) =>
      this.agvs.update((mevcut) => agvUygula(mevcut, agv)),
    );

    baglanti.onreconnected(() => this.connected.set(true));
    baglanti.onclose(() => this.connected.set(false));

    this.connection = baglanti;

    try {
      await baglanti.start();
      this.connected.set(true);
    } catch {
      this.connected.set(false);
      this.error.set('Canli baglanti kurulamadi.');
    }
  }

  async stop(): Promise<void> {
    const baglanti = this.connection;
    this.connection = null;
    this.connected.set(false);
    await baglanti?.stop();
  }
}
