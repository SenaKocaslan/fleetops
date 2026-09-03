import { expect, test } from '@playwright/test';
import { agvSecilebilirOlanaKadarBekle, gorevTamamla } from './yardimcilar';

// Atama akisi: tarayicidan AGV secip gorevi atamak gercekten calisiyor mu?
test.describe('Gorev atama', () => {
  // Her test kendi gorevini olusturur; testler birbirinin verisine
  // bagimli olmaz.
  async function gorevOlustur(page: import('@playwright/test').Page): Promise<string> {
    const malzeme = `MLZ-${Date.now()}-${Math.floor(Math.random() * 1000)}`;

    await page.goto('/');
    await page.getByTestId('material-code').fill(malzeme);
    await page.getByTestId('quantity').fill('2');
    await page.getByTestId('priority').fill('9');
    await page.getByTestId('submit').click();

    await expect(page.getByTestId('task-row').filter({ hasText: malzeme })).toBeVisible();
    return malzeme;
  }

  // Secenek metni bataryayi da iceriyor ("AGV-01 (%95)"); tam etiketle
  // secmek batarya degisince kirilirdi. Kodu iceren secenegin degerini
  // okuyup onunla seciyoruz.
  async function agvSec(satir: import('@playwright/test').Locator, kod: string) {
    const secim = satir.getByTestId('agv-select');
    const deger = await secim.locator('option', { hasText: kod }).getAttribute('value');
    await secim.selectOption(deger!);
  }

  test('secilen AGV goreve atanir ve listede kodu gorunur', async ({ page }) => {
    const malzeme = await gorevOlustur(page);
    await agvSecilebilirOlanaKadarBekle(page, 'AGV-01');
    const satir = page.getByTestId('task-row').filter({ hasText: malzeme });

    await agvSec(satir, 'AGV-01');
    await satir.getByTestId('assign').click();

    // Yenilenen listede ayni satir artik atanmis olmali.
    const atanmis = page.getByTestId('task-row').filter({ hasText: malzeme });
    await expect(atanmis).toContainText('Assigned');
    await expect(atanmis.getByTestId('task-agv')).toHaveText('AGV-01');

    await gorevTamamla(page, malzeme);
  });

  test('AGV secilmeden atama butonu basilamaz', async ({ page }) => {
    const malzeme = await gorevOlustur(page);
    const satir = page.getByTestId('task-row').filter({ hasText: malzeme });

    await expect(satir.getByTestId('assign')).toBeDisabled();
  });

  test('gorev alamayan AGV listede secilemez', async ({ page }) => {
    const malzeme = await gorevOlustur(page);
    const satir = page.getByTestId('task-row').filter({ hasText: malzeme });

    // AGV-03 sarjda ve batarya esigin altinda. Bu karar domain'de
    // veriliyor; buraya kadar dogru tasindigini kontrol ediyoruz.
    const secenekler = await satir.getByTestId('agv-select').locator('option').allTextContents();

    // Belirli bir AGV'nin orada oldugunu iddia etmiyoruz: musaitlik artik
    // olaylarla degisiyor ve baska testler etkileyebilir. Iddia kural:
    // sarjda ve dusuk bataryali AGV asla secenek olamaz.
    expect(secenekler.some((s) => s.includes('AGV-03'))).toBe(false);
    expect(secenekler.length).toBeGreaterThan(1);
  });

  test('atanmis gorevde atama kontrolleri gosterilmez', async ({ page }) => {
    const malzeme = await gorevOlustur(page);
    await agvSecilebilirOlanaKadarBekle(page, 'AGV-02');
    const satir = page.getByTestId('task-row').filter({ hasText: malzeme });

    await agvSec(satir, 'AGV-02');
    await satir.getByTestId('assign').click();

    const atanmis = page.getByTestId('task-row').filter({ hasText: malzeme });
    await expect(atanmis).toContainText('Assigned');
    await expect(atanmis.getByTestId('assign')).toHaveCount(0);

    await gorevTamamla(page, malzeme);
  });
});
