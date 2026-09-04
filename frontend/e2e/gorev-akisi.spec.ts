import { expect, test } from '@playwright/test';
import { girisYap } from './yardimcilar';

test.describe('Gorev havuzu', () => {
  test.beforeEach(async ({ page }) => {
    await girisYap(page);
  });

  test('sayfa acilir ve gorev havuzu gorunur', async ({ page }) => {
    await page.goto('/');

    await expect(page.getByRole('heading', { name: 'FleetOps' })).toBeVisible();
    await expect(page.getByTestId('task-form')).toBeVisible();

    await expect(
      page.getByTestId('task-table').or(page.getByTestId('empty')),
    ).toBeVisible();
    await expect(page.getByTestId('error')).toHaveCount(0);
  });

  test('olusturulan gorev listede gorunur', async ({ page }) => {
    const malzeme = `MLZ-${Date.now()}`;

    await page.goto('/');
    await page.getByTestId('material-code').fill(malzeme);
    await page.getByTestId('quantity').fill('4');
    await page.getByTestId('priority').fill('3');
    await page.getByTestId('submit').click();

    const satir = page.getByTestId('task-row').filter({ hasText: malzeme });
    await expect(satir).toBeVisible();
    await expect(satir).toContainText('Pending');
  });

  test('sunucu tarafi hatasi ekranda gosterilir', async ({ page }) => {
    await page.goto('/');
    await page.getByTestId('material-code').fill('   ');
    await page.getByTestId('submit').click();

    await expect(page.getByTestId('error')).toContainText('Malzeme kodu bos olamaz');
  });

  test('istemci gecersiz miktarda gonderime izin vermez', async ({ page }) => {
    await page.goto('/');
    await page.getByTestId('material-code').fill('MLZ-TEST');
    await page.getByTestId('quantity').fill('0');

    await expect(page.getByTestId('submit')).toBeDisabled();
  });
});
