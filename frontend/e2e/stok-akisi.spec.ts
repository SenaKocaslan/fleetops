import { expect, test } from '@playwright/test';
import { agvSecilebilirOlanaKadarBekle, agvSerbestBirak, girisYap } from './yardimcilar';

test.describe('Stok hareketi', () => {
  test.beforeEach(async ({ page }) => {
    await girisYap(page);

    // Onceki kosulardan kalan atanmis gorevler AGV'leri Busy birakiyor.
    // Test kendi on kosulunu kurmali.
    await agvSerbestBirak(page.request, '11111111-1111-1111-1111-111111111111');
    await agvSerbestBirak(page.request, '22222222-2222-2222-2222-222222222222');
  });

  test('tamamlanan gorev stok hareketine donusur', async ({ page }) => {
    const malzeme = `MLZ-${Date.now()}`;

    await page.goto('/');
    await page.getByTestId('material-code').fill(malzeme);
    await page.getByTestId('quantity').fill('6');
    await page.getByTestId('submit').click();

    await expect(page.getByTestId('task-row').filter({ hasText: malzeme })).toBeVisible();
    await agvSecilebilirOlanaKadarBekle(page, undefined, malzeme);

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
