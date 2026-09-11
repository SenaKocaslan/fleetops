import { expect, test } from '@playwright/test';
import { API, enAzGorevOlustur, girisYap, yetkiliBaslik } from './yardimcilar';

const AGV02 = '22222222-2222-2222-2222-222222222222';

test.describe('Sayfalama', () => {
  // Sayfa boyutu 20; ikinci sayfanin var olmasi icin en az 21 kayit gerek.
  test.beforeAll(async ({ request }) => {
    await enAzGorevOlustur(request, 25);
  });

  test.beforeEach(async ({ page }) => {
    await girisYap(page);
  });

  test('gorev listesi sayfalanir ve sonraki sayfa farkli kayit gosterir', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByTestId('task-pager')).toBeVisible();

    const bilgi = await page.getByTestId('page-info').textContent();
    expect(bilgi).toContain('Sayfa 1 /');

    const ilkSayfaKodlari = await page.getByTestId('task-material').allTextContents();
    expect(ilkSayfaKodlari.length).toBeLessThanOrEqual(20);

    await page.getByTestId('next-page').click();

    await expect(page.getByTestId('page-info')).toContainText('Sayfa 2 /');
    const ikinciSayfaKodlari = await page.getByTestId('task-material').allTextContents();

    // Ayni kayit iki sayfada birden cikmamali.
    expect(ilkSayfaKodlari.filter((k) => ikinciSayfaKodlari.includes(k))).toEqual([]);
  });

  test('durum filtresi yalnizca o durumdaki gorevleri gosterir', async ({ page }) => {
    await page.goto('/');
    await page.getByTestId('task-status-filter').selectOption('Completed');

    // Filtre uygulanana kadar liste bir onceki halini gosterebilir.
    await expect(page.getByTestId('task-status').first()).toHaveAttribute('data-durum', 'Completed');

    // Gorunen metin Turkce ("Tamamlandi"); ham deger data-durum'da.
    const durumlar = await page
      .getByTestId('task-status')
      .evaluateAll((el) => el.map((e) => e.getAttribute('data-durum')));
    expect(durumlar.length).toBeGreaterThan(0);
    expect(durumlar.every((d) => d === 'Completed')).toBe(true);
  });

  test('ilk sayfada onceki butonu kapali', async ({ page }) => {
    await page.goto('/');

    await expect(page.getByTestId('prev-page')).toBeDisabled();
    await page.getByTestId('next-page').click();
    await expect(page.getByTestId('prev-page')).toBeEnabled();
  });

  test('yeni gorev olusturunca ilk sayfaya donulur', async ({ page }) => {
    await page.goto('/');
    await page.getByTestId('next-page').click();
    await expect(page.getByTestId('page-info')).toContainText('Sayfa 2 /');

    const malzeme = `SYF-${Date.now()}`;
    await page.getByTestId('material-code').fill(malzeme);
    await page.getByTestId('submit').click();

    // 2. sayfada kalsaydi kullanici olusturdugu gorevi goremezdi.
    await expect(page.getByTestId('page-info')).toContainText('Sayfa 1 /');
  });
});

test.describe('Alarmlar', () => {
  test.beforeEach(async ({ page }) => {
    await girisYap(page);
  });

  test('dusuk batarya alarmi listede ve rozette gorunur', async ({ page }) => {
    const baslik = await yetkiliBaslik(page.request, 'operator');
    await page.request.post(`${API}/agvs/${AGV02}/telemetry`, {
      headers: baslik,
      data: { batteryLevel: 8 },
    });

    await page.goto('/alarmlar');
    await page.getByTestId('alarm-refresh').click();

    // Kod satirda yazmiyor (grup basliginda); satir data-kod tasiyor.
    const satir = page.locator('[data-testid="alarm-row"][data-kod="Fleet.KritikBatarya"]');
    await expect(satir.filter({ hasText: 'AGV-02' })).toHaveCount(1);

    // Alarmlar ture gore gruplu; siddet grup basliginda.
    const grup = page.getByTestId('alarm-grup').filter({ hasText: 'Fleet.KritikBatarya' });
    await expect(grup).toContainText('Kritik');
    await expect(grup.getByTestId('alarm-grup-adet')).toContainText('alarm');

    await page.goto('/');
    await expect(page.getByTestId('alarm-rozeti')).toBeVisible();
  });

  test('batarya duzelince alarm kaybolur', async ({ page }) => {
    const baslik = await yetkiliBaslik(page.request, 'operator');
    await page.request.post(`${API}/agvs/${AGV02}/telemetry`, {
      headers: baslik,
      data: { batteryLevel: 75 },
    });

    await page.goto('/alarmlar');
    await page.getByTestId('alarm-refresh').click();

    await expect(
      page.locator('[data-testid="alarm-row"][data-kod="Fleet.KritikBatarya"]').filter({ hasText: 'AGV-02' }),
    ).toHaveCount(0);
  });
});
