import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AuthService } from './auth.service';
import { environment } from '../../environments/environment';

const YARIN = new Date(Date.now() + 86_400_000).toISOString();

describe('AuthService', () => {
  let service: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  function girisYap(role = 'Supervisor') {
    service.login('supervisor', 'Supervisor123!').subscribe();
    http.expectOne(`${environment.apiUrl}/auth/login`).flush({
      token: 'jwt-token',
      expiresAtUtc: YARIN,
      userName: 'supervisor',
      role,
    });
  }

  it('basarili girişte token ve rol tutulur', () => {
    girisYap();

    expect(service.girisYapildi()).toBe(true);
    expect(service.token).toBe('jwt-token');
    expect(service.rol()).toBe('Supervisor');
    expect(service.supervisorMu()).toBe(true);
  });

  it('operator rolu supervisor sayilmaz', () => {
    girisYap('Operator');

    expect(service.supervisorMu()).toBe(false);
  });

  it('cikis token ve depoyu temizler', () => {
    girisYap();

    service.logout();

    expect(service.girisYapildi()).toBe(false);
    expect(service.token).toBeNull();
    expect(localStorage.getItem('fleetops.oturum')).toBeNull();
  });

  it('suresi dolmus oturum depodan geri yuklenmez', () => {
    localStorage.setItem(
      'fleetops.oturum',
      JSON.stringify({
        token: 'eski',
        userName: 'u',
        role: 'Operator',
        expiresAtUtc: '2020-01-01T00:00:00Z',
      }),
    );

    // Yeni bir TestBed: servis yeniden olusturulup depodan okusun.
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const yeni = TestBed.inject(AuthService);

    expect(yeni.girisYapildi()).toBe(false);
    expect(yeni.token).toBeNull();
  });

  it('bozuk depo icerigi cokme yerine oturumsuz baslar', () => {
    localStorage.setItem('fleetops.oturum', '{bu json degil');

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    expect(TestBed.inject(AuthService).girisYapildi()).toBe(false);
  });
});
