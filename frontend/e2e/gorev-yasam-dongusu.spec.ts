import { Locator, Page, expect, test } from '@playwright/test';
import {
  agvSecilebilirOlanaKadarBekle,
  agvSerbestBirak,
  durumuBekle,
  girisYap,
} from './yardimcilar';

const AGV01 = '11111111-1111-1111-1111-111111111111';

// Asil sinanan sey arayuzdeki dugmenin kendisi degil, arkasindaki zincir:
// gorev havuza donunce ya da basarisiz olunca arac Fleet'te serbest kalmali.
// Kalmasaydi arac gorev secim listesine bir daha hic donmezdi.
test.describe('Gorev yasam dongusu', () => {
  test.beforeEach(async ({ page }) => {
    await girisYap(page);
    await agvSerbestBirak(page.request, AGV01);
  });

  test('havuza dondurulen gorevin araci yeniden secilebilir olur', async ({ page }) => {
    const malzeme = await gorevOlustur(page);
    await agvSecilebilirOlanaKadarBekle(page, 'AGV-01', malzeme);
    await ata(satir(page, malzeme), 'AGV-01');
    await durumuBekle(satir(page, malzeme), 'Assigned');

    await satir(page, malzeme).getByTestId('release').click();
    await durumuBekle(satir(page, malzeme), 'Pending');

    // Olay outbox uzerinden Fleet'e gidip araci serbest birakana kadar.
    await agvSecilebilirOlanaKadarBekle(page, 'AGV-01', malzeme);
  });

  test('basarisiz bildirilen gorevin araci serbest kalir', async ({ page }) => {
    const malzeme = await gorevOlustur(page);
    await agvSecilebilirOlanaKadarBekle(page, 'AGV-01', malzeme);
    await ata(satir(page, malzeme), 'AGV-01');
    await satir(page, malzeme).getByTestId('start').click();
    await durumuBekle(satir(page, malzeme), 'InProgress');

    page.once('dialog', (d) => d.accept());
    await satir(page, malzeme).getByTestId('fail').click();
    await durumuBekle(satir(page, malzeme), 'Failed');

    // Baska bir bekleyen gorev uzerinden aracin listeye dondugu dogrulaniyor.
    const ikinci = await gorevOlustur(page);
    await agvSecilebilirOlanaKadarBekle(page, 'AGV-01', ikinci);
  });

  test('bekleyen gorev iptal edilir', async ({ page }) => {
    const malzeme = await gorevOlustur(page);

    page.once('dialog', (d) => d.accept());
    await satir(page, malzeme).getByTestId('cancel').click();

    await durumuBekle(satir(page, malzeme), 'Cancelled');
  });

  test('operator havuza dondurme ve iptal dugmelerini gormez', async ({ page }) => {
    await girisYap(page, 'operator');
    await page.goto('/');
    await expect(page.getByTestId('task-search')).toBeVisible();

    await expect(page.getByTestId('release')).toHaveCount(0);
    await expect(page.getByTestId('cancel')).toHaveCount(0);
  });
});

async function gorevOlustur(page: Page): Promise<string> {
  const malzeme = `YD-${Date.now()}-${Math.floor(Math.random() * 1000)}`;

  await page.goto('/');
  await page.getByTestId('material-code').fill(malzeme);
  await page.getByTestId('priority').fill('5');
  await page.getByTestId('submit').click();

  await expect(satir(page, malzeme)).toBeVisible();
  return malzeme;
}

function satir(page: Page, malzeme: string): Locator {
  return page.getByTestId('task-row').filter({ hasText: malzeme });
}

async function ata(s: Locator, kod: string) {
  const secim = s.getByTestId('agv-select');
  const deger = await secim.locator('option', { hasText: kod }).getAttribute('value');
  await secim.selectOption(deger!);
  await s.getByTestId('assign').click();
}
