import { Component, OnDestroy, computed, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FleetLiveService } from './fleet-live.service';

@Component({
  selector: 'app-fleet-live',
  imports: [DatePipe],
  templateUrl: './fleet-live.html',
  styleUrl: './fleet-live.css',
})
export class FleetLive implements OnDestroy {
  private readonly live = inject(FleetLiveService);

  protected readonly agvs = this.live.agvs;
  protected readonly connected = this.live.connected;
  protected readonly error = this.live.error;

  protected readonly musaitSayisi = computed(
    () => this.agvs().filter((a) => a.gorevAlabilir).length,
  );

  constructor() {
    void this.live.start();
  }

  ngOnDestroy(): void {
    void this.live.stop();
  }

  protected bataryaSinifi(seviye: number): string {
    if (seviye < 20) {
      return 'kritik';
    }
    return seviye < 50 ? 'dusuk' : 'iyi';
  }
}
