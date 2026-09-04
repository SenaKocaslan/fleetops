import { expect, test } from '@playwright/test';
import { API, girisYap, yetkiliBaslik } from './yardimcilar';

const AGV02 = '22222222-2222-2222-2222-222222222222';

test.describe('Sayfalama', () => {
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

    const satir = page.getByTestId('alarm-row').filter({ hasText: 'AGV-02' });
    await expect(satir.first()).toContainText('Fleet.KritikBatarya');
    await expect(satir.first()).toContainText('Kritik');

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
      page.getByTestId('alarm-row').filter({ hasText: 'Fleet.KritikBatarya' }).filter({ hasText: 'AGV-02' }),
    ).toHaveCount(0);
  });
});
