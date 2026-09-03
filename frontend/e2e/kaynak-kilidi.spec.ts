import { expect, test } from '@playwright/test';

// Kaynak kilidi akisi tarayicidan calisiyor mu?
test.describe('Kaynak kilitleri', () => {
  // Testler ayni tohum kaynaklari paylasiyor. Her test kendi kaynagini
  // kullanir ve isini bitirince serbest birakir; workers=1 oldugu icin
  // paralel calismiyorlar.
  async function satir(page: import('@playwright/test').Page, kod: string) {
    await page.goto('/kaynaklar');
    await expect(page.getByTestId('resource-table')).toBeVisible();
    return page.getByTestId('resource-row').filter({ hasText: kod });
  }

  async function serbestBirak(page: import('@playwright/test').Page, kod: string) {
    const s = await satir(page, kod);
    if (await s.getByTestId('release').isVisible()) {
      await s.getByTestId('release').click();
      await expect(s.getByTestId('resource-holder')).toHaveText('Serbest');
    }
  }

  test('gezinme baglantisiyla kaynak sayfasina gidilir', async ({ page }) => {
    await page.goto('/');
    await page.getByTestId('nav-resources').click();

    await expect(page.getByRole('heading', { name: 'Paylasilan kaynaklar' })).toBeVisible();
    await expect(page.getByTestId('resource-table')).toBeVisible();
    await expect(page.getByTestId('resource-list-error')).toHaveCount(0);
  });

  test('kaynak kilitlenir ve tutan AGV gorunur', async ({ page }) => {
    await serbestBirak(page, 'CORRIDOR-A');
    const s = await satir(page, 'CORRIDOR-A');

    await expect(s.getByTestId('resource-holder')).toHaveText('Serbest');

    const secim = s.getByTestId('resource-agv-select');
    const deger = await secim.locator('option', { hasText: 'AGV-01' }).getAttribute('value');
    await secim.selectOption(deger!);
    await s.getByTestId('lock').click();

    const kilitli = page.getByTestId('resource-row').filter({ hasText: 'CORRIDOR-A' });
    await expect(kilitli.getByTestId('resource-holder')).toHaveText('AGV-01');

    // Kilitliyken kilitleme kontrolleri yerine birakma butonu var.
    await expect(kilitli.getByTestId('lock')).toHaveCount(0);
    await expect(kilitli.getByTestId('release')).toBeVisible();

    await serbestBirak(page, 'CORRIDOR-A');
  });

  test('AGV secilmeden kilitle butonu basilamaz', async ({ page }) => {
    await serbestBirak(page, 'LIFT-1');
    const s = await satir(page, 'LIFT-1');

    await expect(s.getByTestId('lock')).toBeDisabled();
  });

  test('kilit birakilinca kaynak yeniden serbest olur', async ({ page }) => {
    await serbestBirak(page, 'LIFT-1');
    const s = await satir(page, 'LIFT-1');

    const secim = s.getByTestId('resource-agv-select');
    const deger = await secim.locator('option', { hasText: 'AGV-02' }).getAttribute('value');
    await secim.selectOption(deger!);
    await s.getByTestId('lock').click();

    const kilitli = page.getByTestId('resource-row').filter({ hasText: 'LIFT-1' });
    await expect(kilitli.getByTestId('resource-holder')).toHaveText('AGV-02');

    await kilitli.getByTestId('release').click();

    const serbest = page.getByTestId('resource-row').filter({ hasText: 'LIFT-1' });
    await expect(serbest.getByTestId('resource-holder')).toHaveText('Serbest');
    await expect(serbest.getByTestId('lock')).toBeVisible();
  });
});
