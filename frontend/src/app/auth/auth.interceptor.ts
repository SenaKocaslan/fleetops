import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

// Token'i her istege elle eklemek yerine tek yerde. Unutulan bir servis
// metodu sessizce 401 almaz.
export const authInterceptor: HttpInterceptorFn = (istek, ilerle) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const token = auth.token;

  // Login istegine token eklenmez; henuz yok.
  const yetkili = token
    ? istek.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : istek;

  return ilerle(yetkili).pipe(
    catchError((hata: HttpErrorResponse) => {
      // 401: token yok/gecersiz -> oturumu bitir. 403 farkli: kimlik gecerli,
      // yetki yok; kullaniciyi giris ekranina atmak yaniltici olurdu.
      if (hata.status === 401 && auth.girisYapildi()) {
        auth.logout();
        void router.navigate(['/giris']);
      }

      return throwError(() => hata);
    }),
  );
};
