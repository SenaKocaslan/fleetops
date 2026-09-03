import { expect, test } from '@playwright/test';
import { agvSecilebilirOlanaKadarBekle } from './yardimcilar';

// Gun 6'nin asil kaniti: modul sinirini gecen tam zincir.
// Tarayici -> Tasks modulu -> outbox -> arka plandaki dagitici ->
// Stock modulu -> tarayici. Burada integration testlerden farkli olarak
// ZAMANLAYICI da devrede: dagiticiyi elle cagirmiyoruz.
test.describe('Stok hareketi', () => {
  test('tamamlanan gorev stok hareketine donusur', async ({ page }) => {
    const malzeme = `MLZ-${Date.now()}`;

    await page.goto('/');
    await page.getByTestId('material-code').fill(malzeme);
    await page.getByTestId('quantity').fill('6');
    await page.getByTestId('submit').click();

    await expect(page.getByTestId('task-row').filter({ hasText: malzeme })).toBeVisible();
    await agvSecilebilirOlanaKadarBekle(page);

    const satir = page.getByTestId('task-row').filter({ hasText: malzeme });
    const secim = satir.getByTestId('agv-select');
    const deger = await secim.locator('option').nth(1).getAttribute('value');
    await secim.selectOption(deger!);
    await satir.getByTestId('assign').click();

    await page.getByTestId('task-row').filter({ hasText: malzeme }).getByTestId('start').click();
    await page.getByTestId('task-row').filter({ hasText: malzeme }).getByTestId('complete').click();

    await expect(
      page.getByTestId('task-row').filter({ hasText: malzeme }),
    ).toContainText('Completed');

    // Hareket ANINDA olusmaz: olay once outbox'a yazilir, dagitici bir
    // sonraki turunda teslim eder. Yenileyerek bekliyoruz.
    await page.getByTestId('nav-stock').click();

    await expect(async () => {
      await page.getByTestId('movement-refresh').click();
      await expect(
        page.getByTestId('movement-row').filter({ hasText: malzeme }),
      ).toBeVisible({ timeout: 1000 });
    }).toPass({ timeout: 30000, intervals: [500, 1000, 2000] });

    const hareket = page.getByTestId('movement-row').filter({ hasText: malzeme });
    await expect(hareket).toContainText('6');
  });

  test('stok sayfasi acilir', async ({ page }) => {
    await page.goto('/');
    await page.getByTestId('nav-stock').click();

    await expect(page.getByRole('heading', { name: 'Stok hareketleri' })).toBeVisible();
    await expect(page.getByTestId('movement-list-error')).toHaveCount(0);
  });
});
