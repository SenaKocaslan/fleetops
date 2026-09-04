import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { LoginYaniti, Oturum, ROL_SUPERVISOR, oturumGecerli } from './auth.model';

const DEPO_ANAHTARI = 'fleetops.oturum';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly oturumSinyali = signal<Oturum | null>(this.depodanOku());

  readonly oturum = this.oturumSinyali.asReadonly();
  readonly girisYapildi = computed(() => oturumGecerli(this.oturumSinyali()));
  readonly rol = computed(() => this.oturumSinyali()?.role ?? null);
  readonly supervisorMu = computed(() => this.rol() === ROL_SUPERVISOR);

  login(userName: string, password: string): Observable<LoginYaniti> {
    return this.http
      .post<LoginYaniti>(`${environment.apiUrl}/auth/login`, { userName, password })
      .pipe(tap((yanit) => this.oturumAc(yanit)));
  }

  logout(): void {
    this.oturumSinyali.set(null);
    localStorage.removeItem(DEPO_ANAHTARI);
  }

  get token(): string | null {
    const oturum = this.oturumSinyali();
    return oturumGecerli(oturum) ? oturum!.token : null;
  }

  private oturumAc(yanit: LoginYaniti): void {
    const oturum: Oturum = {
      token: yanit.token,
      userName: yanit.userName,
      role: yanit.role,
      expiresAtUtc: yanit.expiresAtUtc,
    };

    this.oturumSinyali.set(oturum);
    localStorage.setItem(DEPO_ANAHTARI, JSON.stringify(oturum));
  }

  private depodanOku(): Oturum | null {
    const ham = localStorage.getItem(DEPO_ANAHTARI);
    if (!ham) {
      return null;
    }

    try {
      const oturum = JSON.parse(ham) as Oturum;
      // Suresi dolmus oturumu geri yuklemek, kullaniciyi "giris yapmis ama
      // her istegi 401 alan" bir duruma sokar.
      return oturumGecerli(oturum) ? oturum : null;
    } catch {
      return null;
    }
  }
}
