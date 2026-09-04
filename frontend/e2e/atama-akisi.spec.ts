import { expect, test } from '@playwright/test';
import { agvSecilebilirOlanaKadarBekle, agvSerbestBirak, girisYap, gorevTamamla } from './yardimcilar';

test.describe('Gorev atama', () => {
  test.beforeEach(async ({ page }) => {
    await girisYap(page);

    // Onceki kosulardan kalan atanmis gorevler AGV'leri Busy birakiyor.
    // Test kendi on kosulunu kurmali.
    await agvSerbestBirak(page.request, '11111111-1111-1111-1111-111111111111');
    await agvSerbestBirak(page.request, '22222222-2222-2222-2222-222222222222');
  });

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

  async function agvSec(satir: import('@playwright/test').Locator, kod: string) {
    const secim = satir.getByTestId('agv-select');
    const deger = await secim.locator('option', { hasText: kod }).getAttribute('value');
    await secim.selectOption(deger!);
  }

  test('secilen AGV goreve atanir ve listede kodu gorunur', async ({ page }) => {
    const malzeme = await gorevOlustur(page);
    await agvSecilebilirOlanaKadarBekle(page, 'AGV-01', malzeme);
    const satir = page.getByTestId('task-row').filter({ hasText: malzeme });

    await agvSec(satir, 'AGV-01');
    await satir.getByTestId('assign').click();

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

    const secenekler = await satir.getByTestId('agv-select').locator('option').allTextContents();

    expect(secenekler.some((s) => s.includes('AGV-03'))).toBe(false);
    expect(secenekler.length).toBeGreaterThan(1);
  });

  test('atanmis gorevde atama kontrolleri gosterilmez', async ({ page }) => {
    const malzeme = await gorevOlustur(page);
    await agvSecilebilirOlanaKadarBekle(page, 'AGV-02', malzeme);
    const satir = page.getByTestId('task-row').filter({ hasText: malzeme });

    await agvSec(satir, 'AGV-02');
    await satir.getByTestId('assign').click();

    const atanmis = page.getByTestId('task-row').filter({ hasText: malzeme });
    await expect(atanmis).toContainText('Assigned');
    await expect(atanmis.getByTestId('assign')).toHaveCount(0);

    await gorevTamamla(page, malzeme);
  });
});
