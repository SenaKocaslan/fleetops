import { expect, test } from '@playwright/test';
import {
  API,
  agvSecilebilirOlanaKadarBekle,
  agvSerbestBirak,
  girisYap,
  gorevTamamla,
  yetkiliBaslik,
} from './yardimcilar';

const AGV01 = '11111111-1111-1111-1111-111111111111';

test.describe('Canli filo', () => {
  test.beforeEach(async ({ page }) => {
    await girisYap(page);
  });

  test('filo sayfasi acilir ve hub baglanir', async ({ page }) => {
    await page.goto('/filo');

    await expect(page.getByRole('heading', { name: 'Canli filo durumu' })).toBeVisible();
    await expect(page.getByTestId('hub-durumu')).toHaveText('Canli');
    await expect(page.getByTestId('fleet-hata')).toHaveCount(0);
    await expect(page.getByTestId('agv-AGV-01')).toBeVisible();
  });

  test('telemetri sayfa yenilenmeden ekrana yansir', async ({ page }) => {
    await page.goto('/filo');
    await expect(page.getByTestId('hub-durumu')).toHaveText('Canli');

    // Simulator da yaziyor; carismasin diye bilerek simulatorun uretmeyecegi
    // bir deger seciliyor (bosta duran arac icin batarya sabit kalir).
    const yanit = await page.request.post(`${API}/agvs/${AGV01}/telemetry`, {
      headers: await yetkiliBaslik(page.request, 'operator'),
      data: { batteryLevel: 43, locationId: null },
    });
    expect(yanit.status()).toBe(204);

    // page.reload() YOK: deger sunucu itmesiyle gelmeli.
    await expect(page.getByTestId('batarya-AGV-01')).toHaveText('43%');
    await expect(page.getByTestId('gorulme-AGV-01')).not.toHaveText('-');
  });

  test('gorev atamasi agv durumunu canli olarak Busy yapar', async ({ page }) => {
    const malzeme = `CANLI-${Date.now()}`;
    await agvSerbestBirak(page.request, AGV01);

    await page.goto('/');
    await page.getByTestId('material-code').fill(malzeme);
    await page.getByTestId('quantity').fill('2');
    await page.getByTestId('submit').click();

    // Satir gorunene kadar beklenmeli: yardimcinin ilk isi page.reload() ve
    // reload, ucustaki POST'u iptal eder.
    await expect(page.getByTestId('task-row').filter({ hasText: malzeme })).toBeVisible();
    await agvSecilebilirOlanaKadarBekle(page, 'AGV-01', malzeme);

    const satir = page.getByTestId('task-row').filter({ hasText: malzeme });
    const secim = satir.getByTestId('agv-select');
    const deger = await secim.locator('option', { hasText: 'AGV-01' }).getAttribute('value');
    await secim.selectOption(deger!);

    await page.goto('/filo');
    await expect(page.getByTestId('hub-durumu')).toHaveText('Canli');
    await expect(page.getByTestId('agv-AGV-01')).toContainText('Available');

    // Atama baska bir sekmede yapiliyormus gibi: istek dogrudan API'ye gidiyor,
    // filo sayfasi yalnizca hub'dan haber almali.
    const baslik = await yetkiliBaslik(page.request);
    // Havuzda yuzlerce gorev var; sayfa gezmek yerine dogrudan kodla ara.
    const sayfa = await (
      await page.request.get(`${API}/tasks?materialCode=${malzeme}`, { headers: baslik })
    ).json();
    const gorev = sayfa.items.find((g: { materialCode: string }) => g.materialCode === malzeme);
    await page.request.post(`${API}/tasks/${gorev.id}/assign`, {
      headers: baslik,
      data: { agvId: AGV01 },
    });

    // Outbox dagitimi eventual: atama -> outbox -> Fleet handler -> hub.
    await expect(page.getByTestId('agv-AGV-01')).toContainText('Busy', { timeout: 30000 });

    // Temizlik: gorev tamamlanmazsa AGV kalici olarak Busy kalir ve sonraki
    // kosularin altini oyar.
    await page.goto('/');
    await gorevTamamla(page, malzeme);
  });
});
