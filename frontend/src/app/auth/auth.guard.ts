import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const girisGerekli: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.girisYapildi() ? true : router.createUrlTree(['/giris']);
};
