import { Component, computed, effect, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './auth/auth.service';
import { AlarmService } from './alarms/alarm.service';

@Component({
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  private readonly auth = inject(AuthService);
  private readonly alarmService = inject(AlarmService);
  private readonly router = inject(Router);

  protected readonly girisYapildi = this.auth.girisYapildi;
  protected readonly oturum = this.auth.oturum;
  protected readonly kritikAlarm = computed(() => this.alarmService.son().criticalCount);

  constructor() {
    // Alarm rozeti giris yapilmadan cekilemez (401 olurdu). Oturum acilinca
    // bir kez cekiliyor; surekli yoklama olculmus bir problem cozmuyor.
    effect(() => {
      if (this.auth.girisYapildi()) {
        this.alarmService.yenile();
      }
    });
  }

  protected cikis(): void {
    this.auth.logout();
    void this.router.navigate(['/giris']);
  }
}
