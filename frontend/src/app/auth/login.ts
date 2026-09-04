import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';

@Component({
  selector: 'app-login',
  imports: [FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected userName = '';
  protected password = '';
  protected readonly hata = signal<string | null>(null);
  protected readonly gonderiliyor = signal(false);

  protected giris(): void {
    this.hata.set(null);
    this.gonderiliyor.set(true);

    this.auth.login(this.userName, this.password).subscribe({
      next: () => {
        this.gonderiliyor.set(false);
        void this.router.navigate(['/']);
      },
      error: (yanit: { status: number }) => {
        this.gonderiliyor.set(false);
        this.hata.set(
          yanit.status === 401
            ? 'Kullanici adi veya parola hatali.'
            : 'Giris yapilamadi, sunucuya ulasilamiyor.',
        );
      },
    });
  }
}
